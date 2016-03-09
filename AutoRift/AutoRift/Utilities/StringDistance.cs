using System;
using System.Text;

namespace AutoRift.Utilities
{
    internal static class StringDistance
    {
        private const double DefaultMismatchScore = 0.0;
        private const double DefaultMatchScore = 1.0;

        public static double RateSimilarity(string firstWord, string secondWord)
        {
            firstWord = firstWord.Replace("\'", string.Empty).Replace(" ", string.Empty).ToLower();
            secondWord = secondWord.Replace("\'", string.Empty).Replace(" ", string.Empty).ToLower();
            if (firstWord == secondWord)
                return DefaultMatchScore;
            int halfLength = Math.Min(firstWord.Length, secondWord.Length)/2 + 1;

            StringBuilder common1 = GetCommonCharacters(firstWord, secondWord, halfLength);
            int commonMatches = common1.Length;

            if (commonMatches == 0)
                return DefaultMismatchScore;

            StringBuilder common2 = GetCommonCharacters(secondWord, firstWord, halfLength);

            if (commonMatches != common2.Length)
                return DefaultMismatchScore;
            int transpositions = 0;
            for (int i = 0; i < commonMatches; i++)
            {
                if (common1[i] != common2[i])
                    transpositions++;
            }

            transpositions /= 2;
            double jaroMetric = commonMatches/(3.0*firstWord.Length) + commonMatches/(3.0*secondWord.Length) +
                                (commonMatches - transpositions)/(3.0*commonMatches);
            return jaroMetric;
        }

        private static StringBuilder GetCommonCharacters(string firstWord, string secondWord, int separationDistance)
        {
            if ((firstWord == null) || (secondWord == null)) return null;
            StringBuilder returnCommons = new StringBuilder(20);
            StringBuilder copy = new StringBuilder(secondWord);
            int firstWordLength = firstWord.Length;
            int secondWordLength = secondWord.Length;

            for (int i = 0; i < firstWordLength; i++)
            {
                char character = firstWord[i];
                bool found = false;

                for (int j = Math.Max(0, i - separationDistance);
                    !found && j < Math.Min(i + separationDistance, secondWordLength);
                    j++)
                {
                    if (copy[j] == character)
                    {
                        found = true;
                        returnCommons.Append(character);
                        copy[j] = '#';
                    }
                }
            }
            return returnCommons;
        }

        public static double Match(this string s, string t)
        {
            return RateSimilarity(t, s);
        }
    }
}