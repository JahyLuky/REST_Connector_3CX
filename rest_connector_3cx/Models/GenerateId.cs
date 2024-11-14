namespace Chat_3CX_API.Models
{
    public class GenerateId
    {
        private static readonly Random _random = new Random();

        public static string GenerateCustomId(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                                        .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        public static string GenerateUppercaseGuid()
        {
            return Guid.NewGuid().ToString("N").ToUpper();
        }
    }
}
