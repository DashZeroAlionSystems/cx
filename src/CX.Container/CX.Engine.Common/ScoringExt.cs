namespace CX.Engine.Common;

public static class ScoringExt
{
    public static double ScoreOrderVsUsingNDCG(this List<string> resultList, List<string> goalList)
    {
        if (goalList == null || goalList.Count == 0)
            return 0.0;

        // Map each item in the goal list to its relevance score
        var relevanceMap = new Dictionary<string, int>();
        var N = goalList.Count;

        for (var i = 0; i < N; i++)
        {
            // Higher relevance for items at the top of the goal list
            relevanceMap[goalList[i]] = N - i;
        }

        var dcg = 0.0;

        for (var i = 0; i < resultList.Count; i++)
        {
            var item = resultList[i];
            if (!relevanceMap.TryGetValue(item, out var relevance)) 
                continue;
            
            // Compute Discounted Cumulative Gain
            var discount = Math.Log(i + 2, 2); // Log base 2
            dcg += relevance / discount;
        }

        var idcg = 0.0;

        for (var i = 0; i < goalList.Count; i++)
        {
            var relevance = N - i;
            var discount = Math.Log(i + 2, 2); // Log base 2
            idcg += relevance / discount;
        }

        return idcg == 0.0 ? 0.0 : dcg / idcg;
    }
    
    public static double ScoreOrderVsUsingMRR(this List<string> result, List<string> goal)
    {
        if (goal == null || goal.Count == 0)
            return 0.0;

        // Create a set for faster lookup
        HashSet<string> goalSet = [..goal];

        var actualReciprocalSum = 0.0;

        // For each item in the search result, compute its reciprocal rank if it's in the goal list
        for (var i = 0; i < result.Count; i++)
        {
            if (!goalSet.Contains(result[i])) 
                continue;
            
            var rank = i + 1; // ranks start from 1
            actualReciprocalSum += 1.0 / rank;
        }

        // Compute the ideal reciprocal sum (when all goal items are at the top in order)
        var idealReciprocalSum = 0.0;
        for (var i = 1; i <= goal.Count; i++)
        {
            idealReciprocalSum += 1.0 / i;
        }

        // Normalize the score to be between 0 and 1
        var score = actualReciprocalSum / idealReciprocalSum;
        if (score > 1.0)
            score = 1.0; // Ensure the score doesn't exceed 1

        return score;
    }
    
    /// <summary>
    /// n statistics, the Kendall rank correlation coefficient, commonly referred to as Kendall's τ coefficient (after the Greek letter τ, tau), is a statistic used to measure the ordinal association between two measured quantities.
    /// </summary>
    public static double ScoreOrderVsUsingKendallTau(this List<string> search, List<string> goal)
    {
        if (goal == null || goal.Count == 0)
            return 0.0;

        // Map each goal item to its index in the goal list
        var goalIndexMap = new Dictionary<string, int>();
        for (var i = 0; i < goal.Count; i++)
        {
            goalIndexMap[goal[i]] = i;
        }

        // Extract the positions of goal items in the search result
        var searchRanks = new List<int>();
        foreach (var item in search)
        {
            if (goalIndexMap.TryGetValue(item, out var value))
            {
                // Map the item to its index in the goal list
                searchRanks.Add(value);
            }
        }

        var n = goal.Count;
        var totalPairs = n * (n - 1) / 2;

        if (totalPairs == 0)
            return 1.0; // Only one item in goal list

        // Count the number of concordant and discordant pairs
        var concordant = 0;
        var discordant = 0;

        for (var i = 0; i < searchRanks.Count; i++)
        {
            for (var j = i + 1; j < searchRanks.Count; j++)
            {
                if (searchRanks[i] < searchRanks[j])
                    concordant++;
                else if (searchRanks[i] > searchRanks[j])
                    discordant++;
                // If equal, we can choose to ignore or count as concordant
            }
        }

        // For missing items in search result, consider pairs as discordant
        var missingItems = n - searchRanks.Count;
        var missingPairs = missingItems * (missingItems - 1) / 2;
        discordant += missingItems * searchRanks.Count + missingPairs;

        // Calculate the Kendall Tau coefficient
        var tau = (double)(concordant - discordant) / totalPairs;

        // Adjust tau to be between 0 and 1
        var score = (tau + 1) / 2;

        return score;
    }
    
    public static double ScoreBySetSimilarity(this List<string> goalList, List<string> outputList)
    {
        // Create frequency maps for both lists
        var countsGoal = new Dictionary<string, int>();
        foreach (var item in goalList)
        {
            if (!countsGoal.TryAdd(item, 1))
                countsGoal[item]++;
        }

        var countsOutput = new Dictionary<string, int>();
        foreach (var item in outputList)
        {
            if (!countsOutput.TryAdd(item, 1))
                countsOutput[item]++;
        }

        // Get all unique items from both lists
        var uniqueItems = new HashSet<string>(countsGoal.Keys);
        uniqueItems.UnionWith(countsOutput.Keys);

        double totalDifference = 0; // Total difference in counts
        double totalItems = 0;      // Total number of items considered

        foreach (var item in uniqueItems)
        {
            var countGoal = countsGoal.TryGetValue(item, out var value1) ? value1 : 0;
            var countOutput = countsOutput.TryGetValue(item, value: out var value) ? value : 0;

            var difference = Math.Abs(countGoal - countOutput);
            var maxCount = Math.Max(countGoal, countOutput);

            totalDifference += difference;
            totalItems += maxCount;
        }

        // Calculate the score
        var score = totalItems > 0 ? 1 - (totalDifference / totalItems) : 1;

        return score;
    }
    
    public static double ScoreBySetSimilarityWeighted(this  List<string> outputList, List<string> goalList, double notFoundPenalty = 1, bool prorataUnfound = false)
    {
        const double RankWeight = 0.85;
        
        var found = 0.0;
        var foundItems = 0;
        var unfound = 0.0;
        var goalCount = goalList.Count;

        // Create a list to track matched items in goalList
        var matchedGoal = new bool[goalCount];

        // Map each item in goalList to a queue of its indices to handle duplicates
        var goalItemIndices = new Dictionary<string, Queue<int>>();
        for (var i = 0; i < goalList.Count; i++)
        {
            var item = goalList[i];
            if (!goalItemIndices.ContainsKey(item))
            {
                goalItemIndices[item] = new Queue<int>();
            }
            goalItemIndices[item].Enqueue(i);
        }

        var lastFoundWeight = 1.0;
        
        // Process each item in outputList
        for (var idxOut = 0; idxOut < outputList.Count; idxOut++)
        {
            var item = outputList[idxOut];
            if (goalItemIndices.ContainsKey(item) && goalItemIndices[item].Count > 0)
            {
                // Item is in goalList and has unmatched occurrences
                var index = goalItemIndices[item].Dequeue();
                matchedGoal[index] = true;

                var weight = Math.Pow(RankWeight, idxOut);
                found += weight;
                foundItems++;
                lastFoundWeight = weight;
            }
            else
            {
                // Item not in goalList or all instances matched
                // Assign unfound score with index = goalCount
                var weight = Math.Pow(RankWeight, idxOut);
                if (prorataUnfound && goalCount > 0)
                {
                    var prorata = ((double)goalCount - foundItems) / goalCount;
                    weight *= prorata;
                }

                unfound += weight;
            }
        }

        // Add unfound scores for unmatched items in goalList
        for (var i = 0; i < goalCount; i++)
        {
            if (!matchedGoal[i])
            {
                var weight = lastFoundWeight * notFoundPenalty;
                if (prorataUnfound && goalCount > 0)
                {
                    var prorata = ((double)goalCount - foundItems) / goalCount;
                    weight *= prorata;
                }
                unfound += weight;
            }
        }

        // Calculate the final score
        var total = found + unfound;
        var score = total > 0 ? found / total : 1.0;

        return score;
    }
    
    public static double ScoreSortOrders(this Dictionary<string, string> actualSortOrder, Dictionary<string, string> expectedSortOrder)
    {
        // Extract expected properties
        var expectedProperties = expectedSortOrder
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value) && kvp.Value != "NONE")
            .Select(kvp => $"{kvp.Key.ToLower()}:{kvp.Value.ToUpper()}")
            .ToList();

        // Extract actual properties
        var actualProperties = actualSortOrder
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value) && kvp.Value != "NONE" && kvp.Key.ToLower() != "reasoning")
            .Select(kvp => $"{kvp.Key.ToLower()}:{kvp.Value.ToUpper()}")
            .ToList();

        return ComputeNDCG(actualProperties, expectedProperties);
    }

    private static double ComputeNDCG(List<string> resultList, List<string> goalList)
    {
        if (goalList == null || goalList.Count == 0)
            return (resultList?.Count ?? 0) == (goalList?.Count ?? 0) ? 1.0 : 0.0;

        // Map each item in the goal list to its relevance score
        var relevanceMap = new Dictionary<string, int>();
        var N = goalList.Count;

        for (var i = 0; i < N; i++)
        {
            // Higher relevance for items at the top of the goal list
            relevanceMap[goalList[i]] = N - i;
        }

        var dcg = 0.0;

        for (var i = 0; i < resultList.Count; i++)
        {
            var item = resultList[i];
            if (!relevanceMap.TryGetValue(item, out var relevance))
                continue;

            // Compute Discounted Cumulative Gain
            var discount = Math.Log(i + 2, 2); // Log base 2
            dcg += relevance / discount;
        }

        var idcg = 0.0;

        for (var i = 0; i < goalList.Count; i++)
        {
            var relevance = N - i;
            var discount = Math.Log(i + 2, 2); // Log base 2
            idcg += relevance / discount;
        }

        return idcg == 0.0 ? 0.0 : dcg / idcg;
    }

}