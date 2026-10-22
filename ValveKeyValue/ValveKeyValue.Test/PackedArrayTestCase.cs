using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ValveKeyValue.Test
{
    class PackedArrayTestCase
    {
        internal enum ElementAccess
        {
            Indexer,
            AsArraySpan,
            Values,
            Elements,
            Children,
            Enumerator,
        }

        internal abstract class PackedCase
        {
            public abstract Type ElementType { get; }

            public abstract KVValueType ElementValueType { get; }

            public abstract int Length { get; }

            // Packs length values, repeating the test values as needed; the full length packs the shared values array itself.
            public abstract KVObject CreatePacked(KVFlag elementFlag, int length);

            public abstract KVObject CreateListBacked(KVFlag elementFlag, int length);

            public abstract bool PackedSpanEqualsValues(KVObject array, int length);

            public abstract bool PackedSpanIsValuesArray(KVObject array);

            public KVObject CreatePacked(KVFlag elementFlag = KVFlag.None) => CreatePacked(elementFlag, Length);

            public KVObject CreateListBacked(KVFlag elementFlag = KVFlag.None) => CreateListBacked(elementFlag, Length);
        }

        sealed class PackedCase<T> : PackedCase
            where T : unmanaged, IEquatable<T>
        {
            public PackedCase(KVValueType elementValueType, T[] values, Func<T[], KVFlag, KVObject> createPacked, Func<T, KVObject> createElement)
            {
                this.elementValueType = elementValueType;
                this.values = values;
                this.createPacked = createPacked;
                this.createElement = createElement;
            }

            readonly KVValueType elementValueType;
            readonly T[] values;
            readonly Func<T[], KVFlag, KVObject> createPacked;
            readonly Func<T, KVObject> createElement;

            public override Type ElementType => typeof(T);

            public override KVValueType ElementValueType => elementValueType;

            public override int Length => values.Length;

            public override KVObject CreatePacked(KVFlag elementFlag, int length)
            {
                if (length == values.Length)
                {
                    return createPacked(values, elementFlag);
                }

                var repeated = new T[length];

                for (var i = 0; i < length; i++)
                {
                    repeated[i] = values[i % values.Length];
                }

                return createPacked(repeated, elementFlag);
            }

            public override KVObject CreateListBacked(KVFlag elementFlag, int length)
            {
                var array = KVObject.Array(length);

                for (var i = 0; i < length; i++)
                {
                    var element = createElement(values[i % values.Length]);
                    element.Flag = elementFlag;
                    array.Add(element);
                }

                return array;
            }

            // Lengths up to the number of test values only.
            public override bool PackedSpanEqualsValues(KVObject array, int length)
                => array.TryGetArraySpan<T>(out var span) && span.SequenceEqual(values.AsSpan(0, length));

            public override bool PackedSpanIsValuesArray(KVObject array)
            {
                if (!array.TryGetArraySpan<T>(out var span))
                {
                    return false;
                }

                return Unsafe.AreSame(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetArrayDataReference(values));
            }
        }

        static readonly PackedCase[] Cases =
        [
            new PackedCase<bool>(
                KVValueType.Boolean,
                [true, false, true, true, false, false],
                KVObject.PackedArray,
                value => new KVObject(value)),
            new PackedCase<short>(
                KVValueType.Int16,
                [0, 1, -1, short.MinValue, short.MaxValue, 1234],
                KVObject.PackedArray,
                value => new KVObject(value)),
            new PackedCase<ushort>(
                KVValueType.UInt16,
                [0, 1, ushort.MaxValue, 0x8000, 1234, 7],
                KVObject.PackedArray,
                value => new KVObject(value)),
            new PackedCase<int>(
                KVValueType.Int32,
                [0, 1, -1, int.MinValue, int.MaxValue, 123456],
                KVObject.PackedArray,
                value => new KVObject(value)),
            new PackedCase<uint>(
                KVValueType.UInt32,
                [0, 1, uint.MaxValue, 0x80000000, 123456, 7],
                KVObject.PackedArray,
                value => new KVObject(value)),
            new PackedCase<long>(
                KVValueType.Int64,
                [0, 1, -1, long.MinValue, long.MaxValue, 123456789012],
                KVObject.PackedArray,
                value => new KVObject(value)),
            new PackedCase<ulong>(
                KVValueType.UInt64,
                [0, 1, ulong.MaxValue, 0x8000000000000000, 123456789012, 7],
                KVObject.PackedArray,
                value => new KVObject(value)),
            new PackedCase<float>(
                KVValueType.FloatingPoint,
                [0f, -0f, 1.5f, float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.MaxValue, 1e-7f],
                KVObject.PackedArray,
                value => new KVObject(value)),
            new PackedCase<double>(
                KVValueType.FloatingPoint64,
                [0.0, -0.0, 1.5, double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.MaxValue, 1e-300],
                KVObject.PackedArray,
                value => new KVObject(value)),
        ];

        static readonly KVFlag[] ElementFlags = [KVFlag.None, KVFlag.Resource];

        static readonly ElementAccess[] Accesses = Enum.GetValues<ElementAccess>();

        static readonly KVSerializationFormat[] TextFormats = [KVSerializationFormat.KeyValues3Text, KVSerializationFormat.KeyValues1Text];

        static IEnumerable<TestCaseData> AllCases()
        {
            foreach (var testCase in Cases)
            {
                yield return new TestCaseData(testCase).SetArgDisplayNames(testCase.ElementType.Name);
            }
        }

        static IEnumerable<TestCaseData> FlagCases()
        {
            foreach (var testCase in Cases)
            {
                foreach (var flag in ElementFlags)
                {
                    yield return new TestCaseData(testCase, flag).SetArgDisplayNames(testCase.ElementType.Name, flag.ToString());
                }
            }
        }

        static IEnumerable<TestCaseData> AccessCases()
        {
            foreach (var testCase in Cases)
            {
                foreach (var flag in ElementFlags)
                {
                    foreach (var access in Accesses)
                    {
                        yield return new TestCaseData(testCase, flag, access).SetArgDisplayNames(testCase.ElementType.Name, flag.ToString(), access.ToString());
                    }
                }
            }
        }

        static IEnumerable<TestCaseData> SerializationCases()
        {
            foreach (var testCase in Cases)
            {
                foreach (var format in TextFormats)
                {
                    foreach (var flag in ElementFlags)
                    {
                        // Full length is written one value group per line, 3 elements inline, 0 elements empty.
                        int[] lengths = [testCase.Length, 3, 0];

                        foreach (var length in lengths)
                        {
                            var displayLength = length.ToString(CultureInfo.InvariantCulture);
                            yield return new TestCaseData(testCase, format, flag, length).SetArgDisplayNames(testCase.ElementType.Name, format.ToString(), flag.ToString(), displayLength);
                        }
                    }
                }
            }
        }

        #region Packed state

        [TestCaseSource(nameof(AllCases))]
        public void CreatesPackedArrayWithoutUnpacking(PackedCase testCase)
        {
            var array = testCase.CreatePacked();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(array.ValueType, Is.EqualTo(KVValueType.Array));
                Assert.That(array.IsArray, Is.True);
                Assert.That(array.IsCollection, Is.False);
                Assert.That(array.Flag, Is.EqualTo(KVFlag.None));
                Assert.That(array.Count, Is.EqualTo(testCase.Length));
                Assert.That(array.ToString(CultureInfo.InvariantCulture), Is.EqualTo("[Array]"));
                Assert.That(array.Keys, Is.Empty);
                Assert.That(array.ContainsKey("0"), Is.False);
                Assert.That(array.TryGetValue("0", out _), Is.False);
            }

            Assert.That(testCase.PackedSpanEqualsValues(array, testCase.Length), Is.True, "Array was unpacked.");
        }

        [TestCaseSource(nameof(AllCases))]
        public void IsPackedArrayWhilePacked(PackedCase testCase)
        {
            var array = testCase.CreatePacked(KVFlag.Resource);
            var empty = testCase.CreatePacked(KVFlag.None, 0);

            _ = array.Count;
            _ = array.ToString(CultureInfo.InvariantCulture);
            _ = array.ToArray<double>();
            _ = array.GetElement<int>(0);
            Serialize(KVSerializationFormat.KeyValues3Text, WrapInCollection(array));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(array.IsPackedArray, Is.True);
                Assert.That(empty.IsPackedArray, Is.True);
            }
        }

        [TestCaseSource(nameof(AccessCases))]
        public void IsPackedArrayOnlyUntilAsArraySpan(PackedCase testCase, KVFlag elementFlag, ElementAccess access)
        {
            var array = testCase.CreatePacked(elementFlag);

            ReadElements(array, access);

            var expectPacked = access != ElementAccess.AsArraySpan;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(array.IsPackedArray, Is.EqualTo(expectPacked));
                Assert.That(testCase.PackedSpanIsValuesArray(array), Is.EqualTo(expectPacked), "Packed values");
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void IsNotPackedArrayAfterMutation(PackedCase testCase)
        {
            var added = testCase.CreatePacked();
            var removed = testCase.CreatePacked();
            var cleared = testCase.CreatePacked();

            added.Add(new KVObject(1));
            removed.RemoveAt(0);
            cleared.Clear();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(added.IsPackedArray, Is.False);
                Assert.That(removed.IsPackedArray, Is.False);
                Assert.That(cleared.IsPackedArray, Is.False);
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void IsNotPackedArrayForElementArray(PackedCase testCase)
        {
            Assert.That(testCase.CreateListBacked().IsPackedArray, Is.False);
        }

        [Test]
        public void IsNotPackedArrayForOtherValues()
        {
            KVObject[] others =
            [
                KVObject.Array(),
                KVObject.Array(4),
                KVObject.Array([new KVObject(1), new KVObject(2)]),
                KVObject.Blob([1, 2, 3]),
                KVObject.Collection(),
                KVObject.ListCollection(),
                KVObject.Null(),
                new KVObject(1),
                new KVObject("text"),
            ];

            using (Assert.EnterMultipleScope())
            {
                foreach (var other in others)
                {
                    Assert.That(other.IsPackedArray, Is.False, other.ValueType.ToString());
                }
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void PackedArrayStoresTheGivenArray(PackedCase testCase)
        {
            var array = testCase.CreatePacked();

            Assert.That(testCase.PackedSpanIsValuesArray(array), Is.True);
        }

        [TestCaseSource(nameof(AllCases))]
        public void TryGetArraySpanRequiresExactElementType(PackedCase testCase)
        {
            var array = testCase.CreatePacked();

            Assert.That(GetMatchingSpanTypes(array), Is.EqualTo(new[] { testCase.ElementType }));
        }

        [TestCaseSource(nameof(AllCases))]
        public void EmptyPackedArray(PackedCase testCase)
        {
            var array = testCase.CreatePacked(KVFlag.Resource, 0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(array.IsArray, Is.True);
                Assert.That(array.Count, Is.Zero);
                Assert.That(GetMatchingSpanTypes(array), Is.EqualTo(new[] { testCase.ElementType }));
                Assert.That(testCase.PackedSpanEqualsValues(array, 0), Is.True);
            }

            Assert.That(array.AsArraySpan().Length, Is.Zero);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(array.Count, Is.Zero);
                Assert.That(array.Values, Is.Empty);
                Assert.That(array.Children, Is.Empty);
                Assert.That(GetMatchingSpanTypes(array), Is.Empty);
            }
        }

        [Test]
        public void TryGetArraySpanFailsForOtherValues()
        {
            KVObject[] others =
            [
                KVObject.Array(),
                KVObject.Array(4),
                KVObject.Array([new KVObject(1), new KVObject(2)]),
                KVObject.Blob([1, 2, 3]),
                KVObject.Collection(),
                KVObject.ListCollection(),
                KVObject.Null(),
                new KVObject(1),
                new KVObject(1.5f),
                new KVObject("text"),
            ];

            using (Assert.EnterMultipleScope())
            {
                foreach (var other in others)
                {
                    Assert.That(GetMatchingSpanTypes(other), Is.Empty, other.ValueType.ToString());
                    Assert.That(other.TryGetArraySpan<byte>(out _), Is.False, other.ValueType.ToString());
                }
            }
        }

        #endregion

        #region Element reads

        [TestCaseSource(nameof(AccessCases))]
        public void ElementsMatchListBackedArray(PackedCase testCase, KVFlag elementFlag, ElementAccess access)
        {
            var array = testCase.CreatePacked(elementFlag);
            var expected = testCase.CreateListBacked(elementFlag);

            var elements = ReadElements(array, access);

            AssertElementsEqual(elements, ReadElements(expected, access));
            Assert.That(array.Count, Is.EqualTo(testCase.Length));

            // Every later read gives equal elements, whether or not the first access unpacked the array.
            foreach (var otherAccess in Accesses)
            {
                AssertElementsEqual(ReadElements(array, otherAccess), elements);
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void ReadsCreateNewElementsWhilePacked(PackedCase testCase)
        {
            var array = testCase.CreatePacked(KVFlag.Resource);

            var first = array[0];
            first.Flag = KVFlag.SoundEvent;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(array[0], Is.Not.SameAs(first));
                Assert.That(array[0].Flag, Is.EqualTo(KVFlag.Resource));
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void ReadsReturnKeptElementsAfterUnpacking(PackedCase testCase)
        {
            var array = testCase.CreatePacked(KVFlag.Resource);
            var unpacked = array.AsArraySpan()[0];

            foreach (var access in Accesses)
            {
                Assert.That(ReadElements(array, access)[0], Is.SameAs(unpacked), access.ToString());
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void ElementsIndexerAndCountLeaveArrayPacked(PackedCase testCase)
        {
            var packed = testCase.CreatePacked(KVFlag.Resource);
            var expected = testCase.CreateListBacked(KVFlag.Resource);

            var packedElements = packed.Elements;

            var elements = new List<KVObject>();
            for (var i = 0; i < packedElements.Count; i++)
            {
                elements.Add(packedElements[i]);
            }

            AssertElementsEqual(elements, ReadElements(expected, ElementAccess.Indexer));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(packedElements, Has.Count.EqualTo(testCase.Length));
                Assert.That(packed.IsPackedArray, Is.True);
                Assert.Throws<ArgumentOutOfRangeException>(() => _ = packedElements[testCase.Length]);
                Assert.Throws<ArgumentOutOfRangeException>(() => _ = packedElements[-1]);
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void ElementsOfElementArrayIsTheElementList(PackedCase testCase)
        {
            var array = testCase.CreateListBacked();
            var unpacked = testCase.CreatePacked();
            _ = unpacked.AsArraySpan();

            var arrayElements = array.Elements;
            var unpackedElements = unpacked.Elements;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(arrayElements, Is.InstanceOf<List<KVObject>>());
                Assert.That(array.Elements, Is.SameAs(arrayElements));
                Assert.That(array.Values, Is.SameAs(arrayElements));
                Assert.That(unpackedElements, Is.InstanceOf<List<KVObject>>());
                Assert.That(unpacked.Elements, Is.SameAs(unpackedElements));
            }
        }

        [Test]
        public void ElementsIsEmptyForOtherValues()
        {
            KVObject[] others =
            [
                KVObject.Blob([1, 2, 3]),
                KVObject.Collection(),
                KVObject.ListCollection(),
                KVObject.Null(),
                new KVObject(1),
                new KVObject("text"),
            ];

            others[1].Add("key", new KVObject(1));
            others[2].Add("key", new KVObject(1));

            using (Assert.EnterMultipleScope())
            {
                foreach (var other in others)
                {
                    Assert.That(other.Elements, Is.Empty, other.ValueType.ToString());
                }
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void ElementsTakenBeforeUnpackingReadsTheUnpackedElements(PackedCase testCase)
        {
            var array = testCase.CreatePacked(KVFlag.Resource);
            var values = array.Elements;

            var span = array.AsArraySpan();
            var replacement = new KVObject("replaced");
            span[0] = replacement;
            array.Add(new KVObject("extra"));

            var expected = testCase.CreateListBacked(KVFlag.Resource);
            expected.AsArraySpan()[0] = new KVObject("replaced");
            expected.Add(new KVObject("extra"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(array.IsPackedArray, Is.False);
                Assert.That(values, Has.Count.EqualTo(testCase.Length + 1));
                Assert.That(values[0], Is.SameAs(replacement));
                Assert.That(values[testCase.Length].ToString(CultureInfo.InvariantCulture), Is.EqualTo("extra"));
            }

            AssertElementsEqual([.. values], ReadElements(expected, ElementAccess.Indexer));
        }

        [TestCaseSource(nameof(FlagCases))]
        public void ElementsHaveElementValueTypeAndFlag(PackedCase testCase, KVFlag elementFlag)
        {
            var array = testCase.CreatePacked(elementFlag);

            using (Assert.EnterMultipleScope())
            {
                for (var i = 0; i < array.Count; i++)
                {
                    Assert.That(array[i].ValueType, Is.EqualTo(testCase.ElementValueType));
                    Assert.That(array[i].Flag, Is.EqualTo(elementFlag));
                }
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void ArrayFlagIsSeparateFromElementFlag(PackedCase testCase)
        {
            var array = testCase.CreatePacked(KVFlag.Resource);
            array.Flag = KVFlag.SoundEvent;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(array.Flag, Is.EqualTo(KVFlag.SoundEvent));
                Assert.That(array[0].Flag, Is.EqualTo(KVFlag.Resource));
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void IndexerOutOfRangeThrows(PackedCase testCase)
        {
            var array = testCase.CreatePacked();
            var expected = testCase.CreateListBacked();

            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => _ = array[testCase.Length]);
                Assert.Throws<ArgumentOutOfRangeException>(() => _ = array[-1]);
                Assert.Throws<ArgumentOutOfRangeException>(() => _ = expected[testCase.Length]);
                Assert.Throws<ArgumentOutOfRangeException>(() => _ = expected[-1]);
            }

            Assert.That(array.IsPackedArray, Is.True);
        }

        #endregion

        #region Mutation

        [TestCaseSource(nameof(AllCases))]
        public void AddAfterPacking(PackedCase testCase)
        {
            var array = testCase.CreatePacked(KVFlag.Resource);
            var expected = testCase.CreateListBacked(KVFlag.Resource);

            array.Add(new KVObject("extra"));
            expected.Add(new KVObject("extra"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(array.Count, Is.EqualTo(testCase.Length + 1));
                Assert.That(GetMatchingSpanTypes(array), Is.Empty);
            }

            AssertElementsEqual(ReadElements(array, ElementAccess.Indexer), ReadElements(expected, ElementAccess.Indexer));
        }

        [TestCaseSource(nameof(AllCases))]
        public void RemoveAtAfterPacking(PackedCase testCase)
        {
            var array = testCase.CreatePacked(KVFlag.Resource);
            var expected = testCase.CreateListBacked(KVFlag.Resource);

            array.RemoveAt(1);
            expected.RemoveAt(1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(array.Count, Is.EqualTo(testCase.Length - 1));
                Assert.That(GetMatchingSpanTypes(array), Is.Empty);
            }

            AssertElementsEqual(ReadElements(array, ElementAccess.Indexer), ReadElements(expected, ElementAccess.Indexer));
        }

        [TestCaseSource(nameof(AllCases))]
        public void ClearAfterPacking(PackedCase testCase)
        {
            var array = testCase.CreatePacked(KVFlag.Resource);

            array.Clear();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(array.Count, Is.Zero);
                Assert.That(array.IsArray, Is.True);
                Assert.That(GetMatchingSpanTypes(array), Is.Empty);
            }

            array.Add(new KVObject("extra"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(array.Count, Is.EqualTo(1));
                Assert.That((string)array[0], Is.EqualTo("extra"));
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void SpanWritesAfterUnpackingAreVisible(PackedCase testCase)
        {
            var array = testCase.CreatePacked();
            var replacement = new KVObject("replaced");

            array.AsArraySpan()[0] = replacement;

            Assert.That(array[0], Is.SameAs(replacement));
        }

        [TestCaseSource(nameof(AllCases))]
        public void NamedMutationThrowsLikeListBackedArray(PackedCase testCase)
        {
            var array = testCase.CreatePacked();

            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<InvalidOperationException>(() => array.Add("key", new KVObject(1)));
                Assert.Throws<InvalidOperationException>(() => array.Remove("key"));
            }

            Assert.That(testCase.PackedSpanEqualsValues(array, testCase.Length), Is.True, "Array was unpacked.");
        }

        #endregion

        #region Conversion

        [TestCaseSource(nameof(AllCases))]
        public void ConversionsBehaveLikeListBackedArray(PackedCase testCase)
        {
            var array = testCase.CreatePacked();
            var expected = testCase.CreateListBacked();

            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<NotSupportedException>(() => array.ToInt32(CultureInfo.InvariantCulture));
                Assert.Throws<NotSupportedException>(() => expected.ToInt32(CultureInfo.InvariantCulture));
                Assert.Throws<NotSupportedException>(() => array.ToDouble(CultureInfo.InvariantCulture));
                Assert.Throws<NotSupportedException>(() => expected.ToDouble(CultureInfo.InvariantCulture));
                Assert.Throws<InvalidCastException>(() => array.ToType(testCase.ElementType.MakeArrayType(), CultureInfo.InvariantCulture));
                Assert.Throws<InvalidCastException>(() => expected.ToType(testCase.ElementType.MakeArrayType(), CultureInfo.InvariantCulture));
                Assert.That(((IConvertible)array).GetTypeCode(), Is.EqualTo(TypeCode.Object));
            }
        }

        #endregion

        #region Serialization

        [TestCaseSource(nameof(SerializationCases))]
        public void SerializesLikeListBackedArray(PackedCase testCase, KVSerializationFormat format, KVFlag elementFlag, int length)
        {
            var packed = testCase.CreatePacked(elementFlag, length);
            var expected = testCase.CreateListBacked(elementFlag, length);

            Assert.That(Serialize(format, WrapInCollection(packed)), Is.EqualTo(Serialize(format, WrapInCollection(expected))));
            Assert.That(testCase.PackedSpanEqualsValues(packed, length), Is.True, "Serializing unpacked the array.");
        }

        [TestCaseSource(nameof(SerializationCases))]
        public void SerializesWithArrayFlagLikeListBackedArray(PackedCase testCase, KVSerializationFormat format, KVFlag elementFlag, int length)
        {
            var packed = testCase.CreatePacked(elementFlag, length);
            var expected = testCase.CreateListBacked(elementFlag, length);
            packed.Flag = KVFlag.SoundEvent;
            expected.Flag = KVFlag.SoundEvent;

            Assert.That(Serialize(format, WrapInCollection(packed)), Is.EqualTo(Serialize(format, WrapInCollection(expected))));
        }

        [TestCaseSource(nameof(SerializationCases))]
        public void SerializesNestedInArrayLikeListBackedArray(PackedCase testCase, KVSerializationFormat format, KVFlag elementFlag, int length)
        {
            var packed = KVObject.Array([testCase.CreatePacked(elementFlag, length), new KVObject("after")]);
            var expected = KVObject.Array([testCase.CreateListBacked(elementFlag, length), new KVObject("after")]);

            Assert.That(Serialize(format, WrapInCollection(packed)), Is.EqualTo(Serialize(format, WrapInCollection(expected))));
        }

        [TestCaseSource(nameof(FlagCases))]
        public void SerializesAsRootLikeListBackedArray(PackedCase testCase, KVFlag elementFlag)
        {
            var packed = testCase.CreatePacked(elementFlag);
            var expected = testCase.CreateListBacked(elementFlag);

            Assert.That(Serialize(KVSerializationFormat.KeyValues3Text, packed), Is.EqualTo(Serialize(KVSerializationFormat.KeyValues3Text, expected)));
        }

        [TestCaseSource(nameof(FlagCases))]
        public void SerializesWithSourceMapLikeListBackedArray(PackedCase testCase, KVFlag elementFlag)
        {
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var packed = kv.SerializeWithSourceMap(WrapInCollection(testCase.CreatePacked(elementFlag)), "root");
            var expected = kv.SerializeWithSourceMap(WrapInCollection(testCase.CreateListBacked(elementFlag)), "root");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(packed.Text, Is.EqualTo(expected.Text));
                Assert.That(packed.Spans, Is.EqualTo(expected.Spans));
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void BinaryKV1RejectsArraysLikeListBackedArray(PackedCase testCase)
        {
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary);

            using var packedStream = new MemoryStream();
            using var expectedStream = new MemoryStream();

            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<NotImplementedException>(() => kv.Serialize(packedStream, WrapInCollection(testCase.CreatePacked()), "root"));
                Assert.Throws<NotImplementedException>(() => kv.Serialize(expectedStream, WrapInCollection(testCase.CreateListBacked()), "root"));
            }
        }

        #endregion

        #region Thread safety

        const int ConcurrentThreadCount = 8;
        const int ConcurrentIterations = 25;

        // Long enough that threads are still building their lists when others publish.
        const int ConcurrentLength = 4096;

        [TestCaseSource(nameof(AllCases))]
        public void ConcurrentReadsLeaveArrayPacked(PackedCase testCase)
        {
            ElementAccess[] reads = [ElementAccess.Indexer, ElementAccess.Values, ElementAccess.Elements, ElementAccess.Children, ElementAccess.Enumerator];
            var expected = ReadElements(testCase.CreateListBacked(KVFlag.Resource, ConcurrentLength), ElementAccess.Indexer);

            for (var iteration = 0; iteration < ConcurrentIterations; iteration++)
            {
                var array = testCase.CreatePacked(KVFlag.Resource, ConcurrentLength);

                var results = ReadConcurrently(array, reads);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(array.IsPackedArray, Is.True);

                    for (var t = 0; t < results.Length; t++)
                    {
                        Assert.That(results[t], Has.Count.EqualTo(ConcurrentLength));

                        var differentElements = 0;
                        for (var i = 0; i < ConcurrentLength && i < results[t].Count; i++)
                        {
                            var actual = results[t][i];
                            var sameValue = actual.ToString(CultureInfo.InvariantCulture) == expected[i].ToString(CultureInfo.InvariantCulture);

                            if (actual.ValueType != expected[i].ValueType || actual.Flag != expected[i].Flag || !sameValue)
                            {
                                differentElements++;
                            }
                        }

                        Assert.That(differentElements, Is.Zero, $"Thread {t} read elements that differ from the list-backed array.");
                    }
                }
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void ConcurrentAsArraySpanPublishesOneElementList(PackedCase testCase)
        {
            ElementAccess[] unpacks = [ElementAccess.AsArraySpan];
            var expected = ReadElements(testCase.CreateListBacked(KVFlag.Resource, ConcurrentLength), ElementAccess.Indexer);

            for (var iteration = 0; iteration < ConcurrentIterations; iteration++)
            {
                var array = testCase.CreatePacked(KVFlag.Resource, ConcurrentLength);

                var results = ReadConcurrently(array, unpacks);

                var elements = ReadElements(array, ElementAccess.AsArraySpan);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(array.IsPackedArray, Is.False);
                    Assert.That(array.Count, Is.EqualTo(ConcurrentLength));

                    for (var t = 0; t < results.Length; t++)
                    {
                        Assert.That(results[t], Has.Count.EqualTo(ConcurrentLength));

                        var differentElements = 0;
                        for (var i = 0; i < ConcurrentLength; i++)
                        {
                            if (!ReferenceEquals(results[t][i], elements[i]))
                            {
                                differentElements++;
                            }
                        }

                        Assert.That(differentElements, Is.Zero, $"Thread {t} saw elements other than the published ones.");
                    }
                }

                AssertElementsEqual(elements, expected);
            }
        }

        // Reads the elements on several threads at once, cycling through the given accesses.
        static List<KVObject>[] ReadConcurrently(KVObject array, ElementAccess[] accesses)
        {
            var results = new List<KVObject>[ConcurrentThreadCount];
            var failures = new ConcurrentQueue<Exception>();
            var threads = new Thread[ConcurrentThreadCount];

            using var barrier = new Barrier(ConcurrentThreadCount);

            for (var t = 0; t < ConcurrentThreadCount; t++)
            {
                var threadIndex = t;
                var access = accesses[threadIndex % accesses.Length];

                threads[threadIndex] = new Thread(() =>
                {
                    barrier.SignalAndWait();

                    try
                    {
                        results[threadIndex] = ReadElements(array, access);
                    }
#pragma warning disable CA1031 // Rethrowing on a worker thread would crash the test host
                    catch (Exception e)
#pragma warning restore CA1031
                    {
                        failures.Enqueue(e);
                    }
                });
            }

            foreach (var thread in threads)
            {
                thread.Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            Assert.That(failures, Is.Empty);

            return results;
        }

        #endregion

        #region Element conversion

        internal abstract class ConversionTarget
        {
            public abstract Type TargetType { get; }

            // Asserts that GetElement and ToArray on array match the explicit cast of each element of reference.
            public abstract void AssertMatchesCasts(KVObject array, KVObject reference);

            public abstract void AssertUnsupported(KVObject array);
        }

        sealed class ConversionTarget<T> : ConversionTarget
        {
            // Without a cast, only AssertUnsupported can be used.
            public ConversionTarget(Func<KVObject, T>? cast = null)
            {
                this.cast = cast;
            }

            readonly Func<KVObject, T>? cast;

            public override Type TargetType => typeof(T);

            public override void AssertMatchesCasts(KVObject array, KVObject reference)
            {
                var expected = new List<Outcome<T>>();
                for (var i = 0; i < reference.Count; i++)
                {
                    var element = reference[i];
                    expected.Add(Outcome<T>.Of(() => cast!(element)));
                }

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(array.Count, Is.EqualTo(expected.Count));

                    for (var i = 0; i < expected.Count; i++)
                    {
                        var index = i;
                        var actual = Outcome<T>.Of(() => array.GetElement<T>(index));
                        Assert.That(actual.Matches(expected[i]), Is.True, $"GetElement [{i}]: {actual} but the cast gives {expected[i]}");
                    }

                    var actualArray = Outcome<T[]>.Of(array.ToArray<T>);
                    var firstFailure = expected.Find(outcome => outcome.Exception != null);

                    if (firstFailure.Exception != null)
                    {
                        Assert.That(actualArray.Exception, Is.EqualTo(firstFailure.Exception), "ToArray exception");
                    }
                    else
                    {
                        Assert.That(actualArray.Exception, Is.Null, "ToArray exception");
                        var values = actualArray.Value!;
                        Assert.That(values, Has.Length.EqualTo(expected.Count));

                        for (var i = 0; i < expected.Count && i < values.Length; i++)
                        {
                            var actual = new Outcome<T>(values[i], null);
                            Assert.That(actual.Matches(expected[i]), Is.True, $"ToArray [{i}]: {actual} but the cast gives {expected[i]}");
                        }
                    }
                }
            }

            public override void AssertUnsupported(KVObject array)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.Throws<NotSupportedException>(() => array.ToArray<T>());
                    Assert.Throws<NotSupportedException>(() => array.GetElement<T>(0));
                }
            }
        }

        readonly record struct Outcome<T>(T? Value, Type? Exception)
        {
            public static Outcome<T> Of(Func<T> read)
            {
                try
                {
                    return new Outcome<T>(read(), null);
                }
#pragma warning disable CA1031 // The exception type is the outcome being compared
                catch (Exception e)
#pragma warning restore CA1031
                {
                    return new Outcome<T>(default, e.GetType());
                }
            }

            // Compares floating point values by their bits, so -0 and NaN payloads must match too.
            public bool Matches(Outcome<T> other)
            {
                if (Exception != null || other.Exception != null)
                {
                    return Exception == other.Exception;
                }

                if (Value is float single && other.Value is float otherSingle)
                {
                    return BitConverter.SingleToInt32Bits(single) == BitConverter.SingleToInt32Bits(otherSingle);
                }

                if (Value is double number && other.Value is double otherNumber)
                {
                    return BitConverter.DoubleToInt64Bits(number) == BitConverter.DoubleToInt64Bits(otherNumber);
                }

                return EqualityComparer<T>.Default.Equals(Value, other.Value);
            }

            public override string ToString()
            {
                if (Exception != null)
                {
                    return Exception.Name;
                }

                return Convert.ToString(Value, CultureInfo.InvariantCulture) ?? "null";
            }
        }

        static readonly ConversionTarget[] Targets =
        [
            new ConversionTarget<bool>(element => (bool)element),
            new ConversionTarget<byte>(element => (byte)element),
            new ConversionTarget<sbyte>(element => (sbyte)element),
            new ConversionTarget<char>(element => (char)element),
            new ConversionTarget<short>(element => (short)element),
            new ConversionTarget<ushort>(element => (ushort)element),
            new ConversionTarget<int>(element => (int)element),
            new ConversionTarget<uint>(element => (uint)element),
            new ConversionTarget<long>(element => (long)element),
            new ConversionTarget<ulong>(element => (ulong)element),
            new ConversionTarget<float>(element => (float)element),
            new ConversionTarget<double>(element => (double)element),
            new ConversionTarget<decimal>(element => (decimal)element),
            new ConversionTarget<string>(element => (string)element),
        ];

        static readonly ConversionTarget[] UnsupportedTargets =
        [
            new ConversionTarget<DateTime>(),
            new ConversionTarget<IntPtr>(),
            new ConversionTarget<byte[]>(),
            new ConversionTarget<KVObject>(),
            new ConversionTarget<KVValueType>(),
        ];

        static IEnumerable<TestCaseData> ConversionCases()
        {
            foreach (var testCase in Cases)
            {
                foreach (var target in Targets)
                {
                    yield return new TestCaseData(testCase, target).SetArgDisplayNames(testCase.ElementType.Name, target.TargetType.Name);
                }
            }
        }

        static IEnumerable<TestCaseData> TargetCases()
        {
            foreach (var target in Targets)
            {
                yield return new TestCaseData(target).SetArgDisplayNames(target.TargetType.Name);
            }
        }

        static IEnumerable<TestCaseData> UnsupportedTargetCases()
        {
            foreach (var target in UnsupportedTargets)
            {
                yield return new TestCaseData(target).SetArgDisplayNames(target.TargetType.Name);
            }
        }

        // Elements that only an element array can hold, covering every way a cast can fail.
        static KVObject CreateMixedElementArray()
        {
            return KVObject.Array([
                KVObject.Null(),
                new KVObject("12"),
                new KVObject("-3"),
                new KVObject("1.5"),
                new KVObject("abc"),
                new KVObject("x"),
                new KVObject(""),
                new KVObject("1e400"),
                KVObject.Collection(),
                KVObject.ListCollection(),
                KVObject.Array(),
                KVObject.Blob([1, 2]),
                new KVObject(new IntPtr(5)),
                new KVObject(true),
                new KVObject(300),
                new KVObject(-1),
                new KVObject(65),
                new KVObject(1e20),
                new KVObject(-1.5f),
                new KVObject(float.NaN),
                new KVObject(ulong.MaxValue),
                new KVObject(long.MinValue),
            ]);
        }

        [TestCaseSource(nameof(ConversionCases))]
        public void PackedConversionsMatchCasts(PackedCase testCase, ConversionTarget target)
        {
            var packed = testCase.CreatePacked(KVFlag.Resource);
            var reference = testCase.CreateListBacked(KVFlag.Resource);

            target.AssertMatchesCasts(packed, reference);

            Assert.That(testCase.PackedSpanEqualsValues(packed, testCase.Length), Is.True, "Converting unpacked the array.");
        }

        [TestCaseSource(nameof(ConversionCases))]
        public void ElementArrayConversionsMatchCasts(PackedCase testCase, ConversionTarget target)
        {
            var array = testCase.CreateListBacked(KVFlag.Resource);
            var reference = testCase.CreateListBacked(KVFlag.Resource);

            target.AssertMatchesCasts(array, reference);
        }

        [TestCaseSource(nameof(TargetCases))]
        public void MixedElementArrayConversionsMatchCasts(ConversionTarget target)
        {
            var array = CreateMixedElementArray();

            target.AssertMatchesCasts(array, array);
        }

        [TestCaseSource(nameof(TargetCases))]
        public void EmptyArrayConversions(ConversionTarget target)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var testCase in Cases)
                {
                    var packed = testCase.CreatePacked(KVFlag.None, 0);
                    target.AssertMatchesCasts(packed, KVObject.Array());
                }

                target.AssertMatchesCasts(KVObject.Array(), KVObject.Array());
            }
        }

        [TestCaseSource(nameof(UnsupportedTargetCases))]
        public void UnsupportedTargetTypesThrow(ConversionTarget target)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var testCase in Cases)
                {
                    target.AssertUnsupported(testCase.CreatePacked());
                    target.AssertUnsupported(testCase.CreatePacked(KVFlag.None, 0));
                    target.AssertUnsupported(testCase.CreateListBacked());
                }

                target.AssertUnsupported(KVObject.Array());
            }
        }

        [Test]
        public void ConversionOfNonArraysThrows()
        {
            KVObject[] others =
            [
                KVObject.Collection(),
                KVObject.ListCollection(),
                KVObject.Blob([1, 2]),
                KVObject.Null(),
                new KVObject(1),
                new KVObject("1"),
            ];

            using (Assert.EnterMultipleScope())
            {
                foreach (var other in others)
                {
                    Assert.Throws<InvalidOperationException>(() => other.ToArray<int>(), other.ValueType.ToString());
                    Assert.Throws<InvalidOperationException>(() => other.GetElement<int>(0), other.ValueType.ToString());
                }
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void GetElementOutOfRangeThrows(PackedCase testCase)
        {
            var packed = testCase.CreatePacked();
            var elements = testCase.CreateListBacked();

            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => packed.GetElement<int>(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => packed.GetElement<int>(testCase.Length));
                Assert.Throws<ArgumentOutOfRangeException>(() => elements.GetElement<int>(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => elements.GetElement<int>(testCase.Length));
            }

            Assert.That(testCase.PackedSpanEqualsValues(packed, testCase.Length), Is.True, "Array was unpacked.");
        }

        [Test]
        public void ToArrayOfStoredTypeCopiesValues()
        {
            float[] values = [1f, 2f, 3f];
            var packed = KVObject.PackedArray(values);

            var copy = packed.ToArray<float>();
            copy[0] = 100f;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(copy, Is.Not.SameAs(values));
                Assert.That(packed.TryGetArraySpan<float>(out var span), Is.True);
                Assert.That(span[0], Is.EqualTo(1f));
                Assert.That(packed.GetElement<float>(0), Is.EqualTo(1f));
            }
        }

        [Test]
        public void ConversionsReadElementsAddedAfterUnpacking()
        {
            int[] values = [1, 2];
            int[] expected = [1, 2, 3];
            var array = KVObject.PackedArray(values);
            array.Add(new KVObject("3"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(array.ToArray<int>(), Is.EqualTo(expected));
                Assert.That(array.GetElement<float>(2), Is.EqualTo(3f));
            }
        }

        #endregion

        #region Factories

        [Test]
        public void ArrayFactoriesCreateElementArrays()
        {
            KVObject[] elements = [new KVObject(1), new KVObject(2)];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(KVObject.Array().IsPackedArray, Is.False);
                Assert.That(KVObject.Array(4).IsPackedArray, Is.False);
                Assert.That(KVObject.Array(4).Count, Is.Zero);
                Assert.That(KVObject.Array(elements).IsPackedArray, Is.False);
                Assert.That(KVObject.Array(System.Array.Empty<KVObject>()).IsPackedArray, Is.False);
            }
        }

        [Test]
        public void PackedArrayFactoriesStoreTheirElementType()
        {
            int[] ints = [1, 2];
            float[] floats = [1f, 2f];
            double[] doubles = [1.0, 2.0];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(GetMatchingSpanTypes(KVObject.PackedArray(ints)), Is.EqualTo(new[] { typeof(int) }));
                Assert.That(GetMatchingSpanTypes(KVObject.PackedArray(floats, KVFlag.Resource)), Is.EqualTo(new[] { typeof(float) }));
                Assert.That(GetMatchingSpanTypes(KVObject.PackedArray(doubles)), Is.EqualTo(new[] { typeof(double) }));
            }
        }

        [Test]
        public void CollectionExpressionsCreateElements()
        {
            var mixed = KVObject.Array([1, 2.5]);
            var doubles = KVObject.Array([1.0, 2.0]);
            var empty = KVObject.Array([]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(GetMatchingSpanTypes(mixed), Is.Empty);
                Assert.That(mixed[0].ValueType, Is.EqualTo(KVValueType.Int32));
                Assert.That(mixed[1].ValueType, Is.EqualTo(KVValueType.FloatingPoint64));
                Assert.That(GetMatchingSpanTypes(doubles), Is.Empty);
                Assert.That(doubles[0].ValueType, Is.EqualTo(KVValueType.FloatingPoint64));
                Assert.That(GetMatchingSpanTypes(empty), Is.Empty);
                Assert.That(empty.Count, Is.Zero);
            }
        }

        [Test]
        public void NullArgumentsThrow()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<ArgumentNullException>(() => KVObject.Array(null!));
                Assert.Throws<ArgumentNullException>(() => KVObject.PackedArray((bool[])null!));
                Assert.Throws<ArgumentNullException>(() => KVObject.PackedArray((short[])null!));
                Assert.Throws<ArgumentNullException>(() => KVObject.PackedArray((ushort[])null!));
                Assert.Throws<ArgumentNullException>(() => KVObject.PackedArray((int[])null!));
                Assert.Throws<ArgumentNullException>(() => KVObject.PackedArray((uint[])null!));
                Assert.Throws<ArgumentNullException>(() => KVObject.PackedArray((long[])null!));
                Assert.Throws<ArgumentNullException>(() => KVObject.PackedArray((ulong[])null!));
                Assert.Throws<ArgumentNullException>(() => KVObject.PackedArray((float[])null!));
                Assert.Throws<ArgumentNullException>(() => KVObject.PackedArray((double[])null!, KVFlag.Resource));
            }
        }

        #endregion

        #region Helpers

        static List<Type> GetMatchingSpanTypes(KVObject array)
        {
            var types = new List<Type>();
            AddIfPacked<bool>(array, types);
            AddIfPacked<byte>(array, types);
            AddIfPacked<sbyte>(array, types);
            AddIfPacked<char>(array, types);
            AddIfPacked<short>(array, types);
            AddIfPacked<ushort>(array, types);
            AddIfPacked<int>(array, types);
            AddIfPacked<uint>(array, types);
            AddIfPacked<long>(array, types);
            AddIfPacked<ulong>(array, types);
            AddIfPacked<float>(array, types);
            AddIfPacked<double>(array, types);
            return types;
        }

        static void AddIfPacked<T>(KVObject array, List<Type> types)
            where T : unmanaged
        {
            if (array.TryGetArraySpan<T>(out _))
            {
                types.Add(typeof(T));
            }
        }

        static List<KVObject> ReadElements(KVObject array, ElementAccess access)
        {
            var elements = new List<KVObject>();

            switch (access)
            {
                case ElementAccess.Indexer:
                    for (var i = 0; i < array.Count; i++)
                    {
                        elements.Add(array[i]);
                    }
                    break;

                case ElementAccess.AsArraySpan:
                    foreach (var element in array.AsArraySpan())
                    {
                        elements.Add(element);
                    }
                    break;

                case ElementAccess.Values:
                    elements.AddRange(array.Values);
                    break;

                case ElementAccess.Elements:
                    foreach (var element in array.Elements)
                    {
                        elements.Add(element);
                    }
                    break;

                case ElementAccess.Children:
                    foreach (var (key, element) in array.Children)
                    {
                        Assert.That(key, Is.Null);
                        elements.Add(element);
                    }
                    break;

                case ElementAccess.Enumerator:
                    foreach (var (key, element) in array)
                    {
                        Assert.That(key, Is.Null);
                        elements.Add(element);
                    }
                    break;
            }

            return elements;
        }

        static void AssertElementsEqual(List<KVObject> actual, List<KVObject> expected)
        {
            Assert.That(actual, Has.Count.EqualTo(expected.Count));

            using (Assert.EnterMultipleScope())
            {
                for (var i = 0; i < expected.Count; i++)
                {
                    Assert.That(actual[i].ValueType, Is.EqualTo(expected[i].ValueType), $"[{i}] value type");
                    Assert.That(actual[i].Flag, Is.EqualTo(expected[i].Flag), $"[{i}] flag");
                    Assert.That(actual[i].ToString(CultureInfo.InvariantCulture), Is.EqualTo(expected[i].ToString(CultureInfo.InvariantCulture)), $"[{i}] value");
                }
            }
        }

        static KVObject WrapInCollection(KVObject array)
        {
            var root = KVObject.Collection();
            root.Add("before", new KVObject(1));
            root.Add("values", array);
            root.Add("after", new KVObject("end"));
            return root;
        }

        static string Serialize(KVSerializationFormat format, KVObject root)
        {
            var kv = KVSerializer.Create(format);

            using var stream = new MemoryStream();
            kv.Serialize(stream, root, "root");
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        #endregion
    }
}
