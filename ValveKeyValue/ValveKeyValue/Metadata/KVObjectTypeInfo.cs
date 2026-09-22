using System.Collections.Frozen;
using System.Threading;

namespace ValveKeyValue.Metadata
{
    // Maps an object to a KeyValues collection of its members. Members and constructor parameters are
    // resolved on first use, which lets recursive types refer to their own type information.
    sealed class KVObjectTypeInfo<T> : KVTypeInfo<T>
    {
        public KVObjectTypeInfo(
            Func<object?[], T>? create,
            Func<KVParameterInfo[]>? parameters,
            Func<KVMemberInfo<T>[]> members,
            bool constructorSetsRequiredMembers)
        {
            this.create = create;
            this.parameters = parameters;
            this.members = members;
            this.constructorSetsRequiredMembers = constructorSetsRequiredMembers;
        }

        const int StackAllocThreshold = 256;

        static readonly object?[] NoArguments = [];

        readonly Func<object?[], T>? create;
        readonly Func<KVParameterInfo[]>? parameters;
        readonly Func<KVMemberInfo<T>[]> members;
        readonly bool constructorSetsRequiredMembers;

        KVMemberInfo<T>[]? resolvedMembers;
        KVMemberInfo<T>[]? readableMembers;
        ReadPlan? readPlan;

        KVMemberInfo<T>[] Members => resolvedMembers ?? LazyInitializer.EnsureInitialized(
            ref resolvedMembers,
            () => members() ?? throw new InvalidOperationException($"The member factory for '{typeof(T).Name}' returned null."));

        KVMemberInfo<T>[] ReadableMembers => readableMembers ?? LazyInitializer.EnsureInitialized(
            ref readableMembers,
            () => Array.FindAll(Members, static member => member.CanRead));

        ReadPlan Plan => readPlan ?? LazyInitializer.EnsureInitialized(ref readPlan, () => new ReadPlan(this));

        internal override T ReadCore(KVObject value)
        {
            ThrowIfNotCollection(value);

            var plan = Plan;
            return plan.Parameters.Length == 0 ? ReadMembers(value, plan) : ReadWithParameters(value, plan);
        }

        internal override KVObject Write(T value, KVWriteContext context)
        {
            context.Enter(value);

            var readable = ReadableMembers;
            var children = new List<KeyValuePair<string, KVObject>>(readable.Length);

            foreach (var member in readable)
            {
                if (member.WriteValue(value, context) is { } child)
                {
                    children.Add(new KeyValuePair<string, KVObject>(member.Name, child));
                }
            }

            return new KVObject(KVValueType.Collection, children);
        }

        // Creates the instance first and assigns each child to its member as it is read, so the last occurrence of a
        // duplicate key wins.
        T ReadMembers(KVObject value, ReadPlan plan)
        {
            var result = Create(NoArguments);
            var planMembers = plan.Members;

            // Assignments are only tracked when a required member needs to be checked.
            Span<bool> assigned = plan.RequiredMembers.Length == 0
                ? []
                : planMembers.Length <= StackAllocThreshold ? stackalloc bool[planMembers.Length] : new bool[planMembers.Length];

            foreach (var (key, child) in value.EnumerateChildren())
            {
                if (!plan.Lookup.TryGetValue(key, out var index))
                {
                    continue;
                }

                planMembers[index].ReadInto(ref result, child);

                if (!assigned.IsEmpty)
                {
                    assigned[index] = true;
                }
            }

            foreach (var (member, _) in plan.RequiredMembers)
            {
                if (!assigned[member])
                {
                    throw RequiredMemberMissing(planMembers[member]);
                }
            }

            return result;
        }

        // Binds children to constructor parameters, which take precedence, and to writable members, which are assigned
        // after construction in the order in which their keys first occur. The last occurrence of a duplicate key wins.
        T ReadWithParameters(KVObject value, ReadPlan plan)
        {
            var planMembers = plan.Members;
            var arguments = (object?[])plan.MissingValues.Clone();
            var memberValues = new KVObject?[planMembers.Length];

            Span<bool> argumentAssigned = arguments.Length <= StackAllocThreshold ? stackalloc bool[arguments.Length] : new bool[arguments.Length];
            Span<int> order = planMembers.Length <= StackAllocThreshold ? stackalloc int[planMembers.Length] : new int[planMembers.Length];
            var orderCount = 0;

            foreach (var (key, child) in value.EnumerateChildren())
            {
                if (!plan.Lookup.TryGetValue(key, out var index))
                {
                    continue;
                }

                if (index < 0)
                {
                    var parameter = ~index;
                    arguments[parameter] = plan.Parameters[parameter].Type.ReadBoxed(child);
                    argumentAssigned[parameter] = true;
                    continue;
                }

                if (memberValues[index] is null)
                {
                    order[orderCount++] = index;
                }

                memberValues[index] = child;
            }

            var result = Create(arguments);

            foreach (var index in order[..orderCount])
            {
                planMembers[index].ReadInto(ref result, memberValues[index]!);
            }

            foreach (var (member, parameter) in plan.RequiredMembers)
            {
                if (memberValues[member] is null && (parameter < 0 || !argumentAssigned[parameter]))
                {
                    throw RequiredMemberMissing(planMembers[member]);
                }
            }

            return result;
        }

        T Create(object?[] arguments) => create is not null ? create(arguments) : throw NoUsableConstructor(typeof(T).Name);

        static KeyValueException RequiredMemberMissing(KVMemberInfo<T> member)
            => new($"Required property '{member.DeclaredName}' on type '{typeof(T).Name}' was not found in the KeyValues data.");

        sealed class ReadPlan
        {
            public ReadPlan(KVObjectTypeInfo<T> typeInfo)
            {
                Members = typeInfo.Members;
                Parameters = typeInfo.create is not null ? typeInfo.parameters?.Invoke() ?? [] : [];

                var names = new HashSet<string>(Members.Length, StringComparer.OrdinalIgnoreCase);
                var lookup = new Dictionary<string, int>(Members.Length + Parameters.Length, StringComparer.OrdinalIgnoreCase);

                for (var i = 0; i < Members.Length; i++)
                {
                    if (!names.Add(Members[i].Name))
                    {
                        throw new KeyValueException($"Type '{typeof(T).Name}' has more than one member named '{Members[i].Name}'; member names are compared case-insensitively.");
                    }

                    if (Members[i].CanWrite)
                    {
                        lookup[Members[i].Name] = i;
                    }
                }

                // A parameter that corresponds to a member (by declared name) is matched using the member's KeyValues
                // name, so that renamed members bind correctly. A parameter replaces a member with the same name.
                var memberParameters = new int[Members.Length];
                Array.Fill(memberParameters, -1);

                MissingValues = Parameters.Length > 0 ? new object?[Parameters.Length] : [];

                for (var i = 0; i < Parameters.Length; i++)
                {
                    var parameterName = Parameters[i].Name;
                    var memberIndex = Array.FindIndex(Members, m => string.Equals(m.DeclaredName, parameterName, StringComparison.OrdinalIgnoreCase));

                    MissingValues[i] = Parameters[i].MissingValue;

                    if (memberIndex >= 0 && memberParameters[memberIndex] < 0)
                    {
                        memberParameters[memberIndex] = i;
                    }

                    lookup[memberIndex >= 0 ? Members[memberIndex].Name : parameterName] = ~i;
                }

                Lookup = lookup.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

                var requiredMembers = new List<(int, int)>();

                if (!typeInfo.constructorSetsRequiredMembers)
                {
                    for (var i = 0; i < Members.Length; i++)
                    {
                        if (Members[i].IsRequired)
                        {
                            requiredMembers.Add((i, memberParameters[i]));
                        }
                    }
                }

                RequiredMembers = [.. requiredMembers];
            }

            public KVMemberInfo<T>[] Members { get; }

            public KVParameterInfo[] Parameters { get; }

            // The value passed for each parameter that the KeyValues data does not contain.
            public object?[] MissingValues { get; }

            // Maps a KeyValues name to the index of a writable member, or to the complement (~index) of the index of a
            // constructor parameter.
            public FrozenDictionary<string, int> Lookup { get; }

            // Each required member, with the index of the parameter that can assign it instead, or -1.
            public (int Member, int Parameter)[] RequiredMembers { get; }
        }
    }
}
