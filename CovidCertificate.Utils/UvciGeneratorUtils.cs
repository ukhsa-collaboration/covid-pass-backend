using System.Linq;

namespace CovidCertificate.Backend.Utils
{
    public static class UvciGeneratorUtils
    {
        public static char GenerateCheckCharacter(string input)
        {
            const string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789/:";
            int factor = 2;
            int sum = 0;
            int charsetSize = charset.Length;

            foreach (var inputChar in input.Reverse())
            {
                int codePoint = charset.IndexOf(inputChar);

                if (codePoint == -1)
                {
                    continue;
                }

                int addend = factor * codePoint;
                factor = (factor == 2) ? 1 : 2;
                int baseAddend = (addend / charsetSize) + (addend % charsetSize);
                sum += baseAddend;
            }

            int remainder = sum % charsetSize;
            int checkCodePoint = (charsetSize - remainder) % charsetSize;

            return charset[checkCodePoint];
        }
    }
}
