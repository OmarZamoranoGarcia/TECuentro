namespace TEContigo.Shared.Matching
{
    public class TextSimilarity
    {
        /// <summary>
        /// Calcula un porcentaje de similitud (0-100) entre dos textos,
        /// basado en la distancia de Levenshtein normalizada.
        /// Comparación case-insensitive y sin espacios extremos.
        /// </summary>
        public static double CalculateSimilarity(string? a, string? b)
        {
            var textA = (a ?? string.Empty).Trim().ToLowerInvariant();
            var textB = (b ?? string.Empty).Trim().ToLowerInvariant();

            if (textA.Length == 0 && textB.Length == 0)
            {
                return 100.0;
            }

            if (textA.Length == 0 || textB.Length == 0)
            {
                return 0.0;
            }

            if (textA == textB)
            {
                return 100.0;
            }

            var distance = LevenshteinDistance(textA, textB);
            var maxLength = Math.Max(textA.Length, textB.Length);

            var similarity = 1.0 - (double)distance / maxLength;

            return Math.Max(0.0, similarity * 100.0);
        }

        private static int LevenshteinDistance(string a, string b)
        {
            var lengthA = a.Length;
            var lengthB = b.Length;

            var distances = new int[lengthA + 1, lengthB + 1];

            for (var i = 0; i <= lengthA; i++)
            {
                distances[i, 0] = i;
            }

            for (var j = 0; j <= lengthB; j++)
            {
                distances[0, j] = j;
            }

            for (var i = 1; i <= lengthA; i++)
            {
                for (var j = 1; j <= lengthB; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;

                    distances[i, j] = Math.Min(
                        Math.Min(
                            distances[i - 1, j] + 1,
                            distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + cost);
                }
            }

            return distances[lengthA, lengthB];
        }
    }
}
