using System.Collections.Generic;
using Xunit;

namespace Ses.Domain.UnitTests
{
    public class ValueObjectTests
    {
        [Fact]
        public void EqualsWithTwoNullObjectsReturnsTrue()
        {
            const SimpleValueObject obj1 = null;
            const SimpleValueObject obj2 = null;

            var equality = Equals(obj1, obj2);

            Assert.Equal(true, equality);
        }

        [Fact]
        public void EqualsWithNullObjectReturnsFalse()
        {
            const SimpleValueObject obj1 = null;
            var obj2 = new SimpleValueObject();

            var equality = Equals(obj1, obj2);

            Assert.Equal(false, equality);
        }

        [Fact]
        public void EqualsWithTransientObjectsReturnsTrue()
        {
            var obj1 = new SimpleValueObject();
            var obj2 = new SimpleValueObject();

            var equality = Equals(obj1, obj2);

            Assert.Equal(true, equality);
        }

        [Fact]
        public void EqualsWithOneTransientObjectReturnsFalse()
        {
            var obj1 = new SimpleValueObject();
            var obj2 = new SimpleValueObject();

            obj1.SomeText1 = "a";
            obj1.SomeText2 = "b";

            var equality = Equals(obj1, obj2);

            Assert.Equal(false, equality);
        }

        [Fact]
        public void EqualsWithDifferentPropertyValuesReturnsFalse()
        {
            var obj1 = new SimpleValueObject();
            var obj2 = new SimpleValueObject();

            obj1.SomeText1 = "a";
            obj1.SomeText2 = "b";

            obj2.SomeText1 = "x";
            obj2.SomeText2 = "y";

            var equality = Equals(obj1, obj2);

            Assert.Equal(false, equality);
        }

        [Fact]
        public void EqualsWithSamePropertyValuesReturnsTrue()
        {
            var obj1 = new SimpleValueObject();
            var obj2 = new SimpleValueObject();

            obj1.SomeText1 = "a";
            obj1.SomeText2 = "b";

            obj2.SomeText1 = "a";
            obj2.SomeText2 = "b";

            var equality = Equals(obj1, obj2);

            Assert.Equal(true, equality);
        }

        [Fact]
        public void EqualsWithSameIdsPropertyValuesSubclassReturnsFalse()
        {
            var obj1 = new SimpleValueObject();
            var obj2 = new SubSimpleValueObject();

            obj1.SomeText1 = "a";
            obj1.SomeText2 = "b";

            obj2.SomeText1 = "a";
            obj2.SomeText2 = "b";

            var equality = Equals(obj1, obj2);

            Assert.Equal(false, equality);
        }

        [Fact]
        public void EqualsWithDifferentPropertyValuesInDisparateClassesReturnsFalse()
        {
            var obj1 = new SimpleValueObject();
            var obj2 = new OtherSimpleValueObject();

            obj1.SomeText1 = "a";
            obj1.SomeText2 = "b";

            obj2.SomeText1 = "x";
            obj2.SomeText2 = "y";


            var equality = Equals(obj1, obj2);

            Assert.Equal(false, equality);
        }

        [Fact]
        public void EqualsWithSameIdsInDisparateClassesReturnsFalse()
        {
            var obj1 = new SimpleValueObject();
            var obj2 = new OtherSimpleValueObject();

            obj1.SomeText1 = "a";
            obj1.SomeText2 = "b";

            obj2.SomeText1 = "a";
            obj2.SomeText2 = "b";

            var equality = Equals(obj1, obj2);

            Assert.Equal(false, equality);
        }

        public class SimpleValueObject : ValueObject
        {
            public string SomeText1 { get; set; }
            public string SomeText2 { get; set; }

            protected override IEnumerable<object> GetAtomicValues()
            {
                return new[] { SomeText1, SomeText2 };
            }
        }

        public class SubSimpleValueObject : SimpleValueObject
        {
        }

        public class OtherSimpleValueObject : ValueObject
        {
            public string SomeText1 { get; set; }
            public string SomeText2 { get; set; }

            protected override IEnumerable<object> GetAtomicValues()
            {
                return new[] { SomeText1, SomeText2 };
            }
        }
    }
}