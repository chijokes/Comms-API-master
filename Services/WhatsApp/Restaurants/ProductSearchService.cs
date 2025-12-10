using FusionComms.Data;
using FusionComms.Entities.WhatsApp;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FusionComms.Services.WhatsApp.Restaurants
{
public class ProductSearchService
{
    private readonly AppDbContext _db;

    public ProductSearchService(AppDbContext dbContext)
    {
        _db = dbContext;
    }

    public static class SearchMessages
    {
        public const string SEARCH_PROMPT = "🔍 *Search Menu*\n\nType what you're looking for (e.g., 'burger', 'pizza', 'chicken'):";
        public const string NO_SEARCH_RESULTS = "❌ No items found matching *{0}*.\n\nTry different keywords or browse the full menu.";
    }

    public async Task<List<WhatsAppProduct>> SearchProductsAsync(string businessId, string revenueCenterId, string searchQuery, int maxResults = 30, int maxDistance = 2)
    {
        if (string.IsNullOrWhiteSpace(searchQuery) || searchQuery.Length < 2)
        {
            return new List<WhatsAppProduct>();
        }

        var filteredProducts = await _db.WhatsAppProducts
            .Where(p => p.ProductSet.BusinessId == businessId &&
                        p.RevenueCenterId == revenueCenterId &&
                        p.ProductSet != null)
            .Include(p => p.ProductSet)
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();

        if (filteredProducts.Count == 0)
        {
            return new List<WhatsAppProduct>();
        }

        var normalizedQuery = NormalizeForSearch(searchQuery);
        var rawTokens = Tokenize(normalizedQuery);

        if (rawTokens.Count == 0)
            return new List<WhatsAppProduct>();

        var stopWords = new HashSet<string> {
            "i", "want", "just", "and", "&", "with", "the", "a", "an", "on",
            "in", "please", "also", "for", "to", "of", "is", "are", "my", "me",
            "need", "get", "can", "give", "show", "list", "any", "some", "that",
            "do", "you", "have"
        };
        
        var sizeWords = new HashSet<string> { 
            "small", "sm", "medium", "md", "large", "lg", "xl", "extra", 
            "regular", "family", "combo", "meal", "monsterito", "standard"
        };
        
        var queryTokens = rawTokens
            .Where(t => t.Length >= 2 && !stopWords.Contains(t) && !sizeWords.Contains(t))
            .ToList();

        if (queryTokens.Count == 0)
            return new List<WhatsAppProduct>();

        var results = new List<(WhatsAppProduct Product, int Score, int DistanceSum, int MatchedCount)>();

        foreach (var product in filteredProducts)
        {
            var nameNormalized = NormalizeForSearch(product.Name);
            var nameTokens = Tokenize(nameNormalized);

            int matchedCount = 0;
            int distanceSum = 0;
            int score = 0;

            var phrase = string.Join(" ", queryTokens);
            bool exactPhraseMatch = nameNormalized.Contains(phrase);
            if (exactPhraseMatch)
            {
                score += 100;
                matchedCount = queryTokens.Count;
            }
            else
            {
                foreach (var token in queryTokens)
                {
                    bool tokenMatched = false;
                    int bestDistance = int.MaxValue;

                    foreach (var nameToken in nameTokens)
                    {
                        if (nameToken.Length < 2) continue;

                        if (nameToken.Equals(token, StringComparison.OrdinalIgnoreCase))
                        {
                            tokenMatched = true;
                            bestDistance = 0;
                            score += 10;
                            break;
                        }
                        
                        if (nameToken.Contains(token) || token.Contains(nameToken))
                        {
                            tokenMatched = true;
                            bestDistance = 0;
                            score += 5;
                            break;
                        }
                        
                        var distance = CalculateLevenshteinDistance(token, nameToken);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                        }
                    }

                    if (!tokenMatched)
                    {
                        var fullNameDistance = CalculateLevenshteinDistance(token, nameNormalized);
                        if (fullNameDistance <= maxDistance)
                        {
                            tokenMatched = true;
                            bestDistance = fullNameDistance;
                            score += 3;
                        }
                    }

                    if (tokenMatched && bestDistance <= maxDistance)
                    {
                        matchedCount++;
                        distanceSum += bestDistance;
                    }
                }

                if (matchedCount == queryTokens.Count)
                {
                    score += 20;
                }
                else if (matchedCount >= queryTokens.Count / 2)
                {
                    score += 10;
                }
            }

            if (matchedCount > 0)
            {
                results.Add((product, score, distanceSum, matchedCount));
            }
        }

        var finalResults = results
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.DistanceSum)
            .ThenByDescending(r => r.MatchedCount)
            .ThenBy(r => r.Product.Name)
            .Take(maxResults)
            .Select(r => r.Product)
            .ToList();

        return finalResults;
    }

    private static string NormalizeForSearch(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input.ToLower().Trim();

        s = RemoveParenthesesContent(s);

        var chars = s.Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ').ToArray();
        s = new string(chars);

        while (s.Contains("  ")) s = s.Replace("  ", " ");

        return s.Trim();
    }

    private static string RemoveParenthesesContent(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var result = new List<char>(s.Length);
        int depth = 0;
        foreach (var ch in s)
        {
            if (ch == '(') { depth++; continue; }
            if (ch == ')') { if (depth > 0) depth--; continue; }
            if (depth == 0) result.Add(ch);
        }
        return new string(result.ToArray());
    }

    private static List<string> Tokenize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return new List<string>();
        return input
            .Split(' ', '\t', '\r', '\n', '-', '_')
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .ToList();
    }

    public bool IsSearchQuery(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var nonSearchPatterns = new[] {
            "START_ORDER", "GET_HELP", "CONFIRM_ORDER", "EDIT_ORDER", "CANCEL_ORDER",
            "ADD_ITEM", "REMOVE_ITEM", "BACK_TO_SUMMARY", "PROCEED_CHECKOUT",
            "PROFILE_", "MANAGE_PROFILE", "SEARCH", "FULL_MENU", "BACK_TO_MAIN",
            "CAT_", "SUBCAT_", "CAT_SET_", "VIEW_MORE_CATEGORIES",
            "DELIVERY", "PICKUP", "LOCATION_NOT_LISTED"
        };

        if (nonSearchPatterns.Any(pattern =>
            message.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
            message.StartsWith(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return message.Length >= 2 && message.Length <= 50;
    }

    private int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target)) return source.Length;

        var sourceLength = source.Length;
        var targetLength = target.Length;

        var distance = new int[sourceLength + 1, targetLength + 1];

        for (int i = 0; i <= sourceLength; distance[i, 0] = i++) { }
        for (int j = 0; j <= targetLength; distance[0, j] = j++) { }

        for (int i = 1; i <= sourceLength; i++)
        {
            for (int j = 1; j <= targetLength; j++)
            {
                var cost = target[j - 1] == source[i - 1] ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[sourceLength, targetLength];
    }
}
}
