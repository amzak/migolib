namespace MigoLib.Tests
{
    public class SerializerTestModel
    {
        public const string FixedString = "testmodel";

        public int FieldA { get; set; }
        public float FieldB { get; set; }
        public string FieldC { get; set; }
        public bool FieldD { get; set; }
        public double FieldE { get; set; }

        public string GetStringRepresentation()
        {
            var expected = Expected;
            return $"{FixedString}:{expected.FieldA};{expected.FieldB};{expected.FieldC},{expected.FieldD},{expected.FieldE}";
        }

        public static SerializerTestModel Expected => new()
        {
            FieldA = 1,
            FieldB = 3.14f,
            FieldC = "qwerty",
            FieldD = false,
            FieldE = 6.28
        };
    }
}