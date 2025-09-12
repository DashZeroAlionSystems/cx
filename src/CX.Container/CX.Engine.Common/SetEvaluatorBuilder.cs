using System;
using System.Collections.Generic;
using System.Linq;

namespace CX.Engine.Common
{
    /// <summary>
    /// Represents the different ways to measure how well two lists match or
    /// how well an actual sort order matches an expected sort order.
    /// </summary>
    public enum ScoringMethod
    {
        /// <summary>
        /// Normalized Discounted Cumulative Gain:
        /// Measures how well the results match an ideal ranking,
        /// placing more weight on items at the top of the list.
        /// </summary>
        NDCG,

        /// <summary>
        /// Mean Reciprocal Rank:
        /// Focuses on the position of the first relevant item,
        /// often used in question answering or web search scenarios.
        /// </summary>
        MRR,

        /// <summary>
        /// Kendall's Tau:
        /// A correlation measure for two orderings,
        /// counting how many pairwise orders are the same vs. reversed.
        /// </summary>
        KendallTau,

        /// <summary>
        /// Simple Set Similarity:
        /// Compares two collections as sets (or multisets)
        /// to see how many items overlap vs. how many differ.
        /// </summary>
        SetSimilarity,

        /// <summary>
        /// Weighted Set Similarity:
        /// A variant of set similarity that takes into account
        /// item positions and custom penalties for missing/extra items.
        /// </summary>
        WeightedSetSimilarity,

        /// <summary>
        /// Sort Orders:
        /// Compares actual sorting instructions (like "price ASC, year DESC")
        /// against an expected sort order using NDCG or a similar approach.
        /// </summary>
        SortOrders
    }

    /// <summary>
    /// The interface that any SetEvaluator must implement.
    /// </summary>
    public interface ISetEvaluator
    {
        /// <summary>
        /// Evaluate how closely a resultSet matches a referenceSet,
        /// or how actual sort instructions match expected sort instructions
        /// (if using SortOrders).
        /// 
        /// Note: Both referenceSet and resultSet will be truncated via OnlyTop(n)
        /// before evaluation if configured.
        ///
        /// The higher the return value, presumably the closer the match.
        /// </summary>
        double Evaluate(
            IList<string> referenceSet, 
            IList<string> resultSet, 
            Dictionary<string, string> actualSortOrder = null
        );
    }

    /// <summary>
    /// The builder that configures and constructs an ISetEvaluator.
    /// </summary>
    public class SetEvaluatorBuilder
    {
        private ScoringMethod _method = ScoringMethod.SetSimilarity;
        private int? _onlyTop;
        private double _weightMultiplierPerRank = 1.0;
        private double _penaltyRefNotInResult = 0.0;
        private double _penaltyNotInRef = 0.0;

        // Optional: If we want to compare actual vs expected sort orders:
        private Dictionary<string, string> _expectedSortOrder = null;

        /// <summary>
        /// Choose which scoring method to use.
        /// </summary>
        public SetEvaluatorBuilder UseMethod(ScoringMethod method)
        {
            _method = method;
            return this;
        }

        /// <summary>
        /// Only consider the top n items in both the Reference Set and the Result Set.
        /// </summary>
        public SetEvaluatorBuilder OnlyTop(int n)
        {
            _onlyTop = n;
            return this;
        }

        /// <summary>
        /// Multiply the "weight" of each item by a factor as its rank increases.
        /// E.g., 1.1 means each subsequent item is worth 1.1 times the item before it.
        /// (Used in WeightedSetSimilarity, etc.)
        /// </summary>
        public SetEvaluatorBuilder WeightMultiplierPerRank(double multiplier)
        {
            _weightMultiplierPerRank = multiplier;
            return this;
        }

        /// <summary>
        /// If an item is in the Reference Set but not in the Result Set, how much to penalize?
        /// </summary>
        public SetEvaluatorBuilder PenaltyForItemsInReferenceSetNotInResultSet(double penalty)
        {
            _penaltyRefNotInResult = penalty;
            return this;
        }

        /// <summary>
        /// If an item is in the Result Set but NOT in the Reference Set, how much to penalize?
        /// </summary>
        public SetEvaluatorBuilder PenaltyForItemsNotInReferenceSet(double penalty)
        {
            _penaltyNotInRef = penalty;
            return this;
        }

        /// <summary>
        /// If you want to compare SortOrders (ScoringMethod.SortOrders),
        /// you can set the expected dictionary here.
        /// </summary>
        public SetEvaluatorBuilder SetExpectedSortOrder(Dictionary<string, string> expected)
        {
            _expectedSortOrder = expected;
            return this;
        }

        /// <summary>
        /// Construct the actual evaluator with the configured parameters.
        /// </summary>
        public ISetEvaluator Build()
        {
            return new SetEvaluator(
                _method,
                _onlyTop,
                _weightMultiplierPerRank,
                _penaltyRefNotInResult,
                _penaltyNotInRef,
                _expectedSortOrder
            );
        }

        /// <summary>
        /// Shortcut so you can do:
        /// new SetEvaluatorBuilder().UseMethod(...).Score(refSet, resultSet)
        /// </summary>
        public double Score(
            IList<string> referenceSet, 
            IList<string> resultSet, 
            Dictionary<string, string> actualSortOrder = null
        )
        {
            return Build().Evaluate(referenceSet, resultSet, actualSortOrder);
        }
    }

    /// <summary>
    /// The actual SetEvaluator that does the "scoring" based on builder parameters
    /// and the chosen ScoringMethod.
    /// 
    /// Note: We always apply OnlyTop() to BOTH referenceSet and resultSet
    /// inside Evaluate(...).
    /// </summary>
    internal class SetEvaluator : ISetEvaluator
    {
        private readonly ScoringMethod _method;
        private readonly int? _onlyTop;
        private readonly double _weightMultiplierPerRank;
        private readonly double _penaltyRefNotInResult;
        private readonly double _penaltyNotInRef;
        private readonly Dictionary<string, string> _expectedSortOrder;

        public SetEvaluator(
            ScoringMethod method,
            int? onlyTop,
            double weightMultiplierPerRank,
            double penaltyRefNotInResult,
            double penaltyNotInRef,
            Dictionary<string, string> expectedSortOrder
        )
        {
            _method = method;
            _onlyTop = onlyTop;
            _weightMultiplierPerRank = weightMultiplierPerRank;
            _penaltyRefNotInResult = penaltyRefNotInResult;
            _penaltyNotInRef = penaltyNotInRef;
            _expectedSortOrder = expectedSortOrder;
        }

        /// <summary>
        /// Evaluate how closely a resultSet matches a referenceSet,
        /// or how actual sort instructions match expected sort instructions.
        /// 
        /// Both the referenceSet and the resultSet are truncated
        /// via OnlyTop(n) (if specified) before applying the scoring method.
        /// </summary>
        public double Evaluate(
            IList<string> referenceSet, 
            IList<string> resultSet, 
            Dictionary<string, string> actualSortOrder = null
        )
        {
            // Handle null inputs
            if (referenceSet == null) referenceSet = new List<string>();
            if (resultSet == null) resultSet = new List<string>();

            // Truncate both sets if OnlyTop is configured
            var truncatedReference = ApplyOnlyTop(referenceSet);
            var truncatedResult = ApplyOnlyTop(resultSet);

            switch (_method)
            {
                case ScoringMethod.NDCG:
                    return EvaluateNDCG(truncatedResult, truncatedReference);

                case ScoringMethod.MRR:
                    return EvaluateMRR(truncatedResult, truncatedReference);

                case ScoringMethod.KendallTau:
                    return EvaluateKendallTau(truncatedResult, truncatedReference);

                case ScoringMethod.WeightedSetSimilarity:
                    return EvaluateWeightedSetSimilarity(truncatedResult, truncatedReference);

                case ScoringMethod.SortOrders:
                    // Compare actual vs. expected sort instructions
                    var actualSort = actualSortOrder ?? new Dictionary<string, string>();
                    var expectedSort = _expectedSortOrder ?? new Dictionary<string, string>();
                    return EvaluateSortOrders(actualSort, expectedSort);

                case ScoringMethod.SetSimilarity:
                default:
                    // Default to simpler set-based approach
                    return EvaluateSimpleSet(truncatedReference, truncatedResult);
            }
        }

        #region Private Scoring Logic

        // Helper to apply OnlyTop(n) to a list
        private IList<string> ApplyOnlyTop(IList<string> list)
        {
            if (_onlyTop.HasValue && _onlyTop.Value > 0 && _onlyTop.Value < list.Count)
                return list.Take(_onlyTop.Value).ToList();
            return list;
        }

        // ------------------- 1) Simple Set Similarity (DEFAULT) -------------------
        private double EvaluateSimpleSet(IList<string> referenceSet, IList<string> resultList)
        {
            // Convert to sets to compare membership
            var refSet = new HashSet<string>(referenceSet);
            var resSet = new HashSet<string>(resultList);

            // Count matches
            var matches = refSet.Intersect(resSet).Count();

            // If reference is empty, handle carefully
            if (referenceSet.Count == 0)
                return (resSet.Count == 0) ? 1.0 : 0.0;

            // Simple ratio: matched / referenceSetCount
            return (double)matches / referenceSet.Count;
        }

        // ------------------- 2) WeightedSetSimilarity -------------------
        private double EvaluateWeightedSetSimilarity(IList<string> resultList, IList<string> goalList)
        {
            // You can adjust this exponent base if desired
            var rankBase = 0.85;

            var found = 0.0;
            var foundItems = 0;
            var unfound = 0.0;
            var goalCount = goalList.Count;

            // Keep track of which goal items got matched
            var matchedGoal = new bool[goalCount];

            // Map each item in goalList to a queue of its indices
            var goalItemIndices = new Dictionary<string, Queue<int>>();
            for (var i = 0; i < goalCount; i++)
            {
                var item = goalList[i];
                if (!goalItemIndices.ContainsKey(item))
                    goalItemIndices[item] = new Queue<int>();
                goalItemIndices[item].Enqueue(i);
            }

            var lastFoundWeight = 1.0;

            for (var idx = 0; idx < resultList.Count; idx++)
            {
                var item = resultList[idx];
                // Combine rankBase with user-defined multiplier
                var weight = Math.Pow(rankBase, idx) * Math.Pow(_weightMultiplierPerRank, idx);

                if (goalItemIndices.ContainsKey(item) && goalItemIndices[item].Count > 0)
                {
                    // Matched
                    var gIndex = goalItemIndices[item].Dequeue();
                    matchedGoal[gIndex] = true;
                    found += weight;
                    foundItems++;
                    lastFoundWeight = weight;
                }
                else
                {
                    // Extra item => penalty
                    var penaltyWeight = weight * _penaltyNotInRef;
                    unfound += penaltyWeight;
                }
            }

            // Now penalize any missing items from the reference
            for (var i = 0; i < goalCount; i++)
            {
                if (!matchedGoal[i])
                {
                    unfound += lastFoundWeight * _penaltyRefNotInResult;
                }
            }

            var total = found + unfound;
            if (total <= 0.0) return 1.0; // avoid negative or zero
            return found / total;
        }

        // ------------------- 3) NDCG -------------------
        private double EvaluateNDCG(IList<string> resultList, IList<string> goalList)
        {
            if (goalList.Count == 0) return 0.0;

            // Give higher relevance to items near the start of goalList
            var relevanceMap = new Dictionary<string, int>();
            var N = goalList.Count;
            for (var i = 0; i < N; i++)
            {
                relevanceMap[goalList[i]] = N - i;
            }

            var dcg = 0.0;
            for (var i = 0; i < resultList.Count; i++)
            {
                var item = resultList[i];
                if (relevanceMap.TryGetValue(item, out var relevance))
                {
                    var discount = Math.Log(i + 2, 2); // log base 2
                    dcg += relevance / discount;
                }
            }

            var idcg = 0.0;
            for (var i = 0; i < N; i++)
            {
                var relevance = N - i;
                var discount = Math.Log(i + 2, 2);
                idcg += relevance / discount;
            }

            return (idcg == 0.0) ? 0.0 : dcg / idcg;
        }

        // ------------------- 4) MRR -------------------
        private double EvaluateMRR(IList<string> resultList, IList<string> goalList)
        {
            if (goalList.Count == 0) return 0.0;

            var goalSet = new HashSet<string>(goalList);
            var actualReciprocalSum = 0.0;

            for (var i = 0; i < resultList.Count; i++)
            {
                if (goalSet.Contains(resultList[i]))
                {
                    var rank = i + 1;
                    actualReciprocalSum += 1.0 / rank;
                }
            }

            // Ideal reciprocal sum
            var idealReciprocalSum = 0.0;
            for (var i = 1; i <= goalList.Count; i++)
            {
                idealReciprocalSum += 1.0 / i;
            }

            var score = actualReciprocalSum / idealReciprocalSum;
            return (score > 1.0) ? 1.0 : score;
        }

        // ------------------- 5) KendallTau -------------------
        private double EvaluateKendallTau(IList<string> resultList, IList<string> goalList)
        {
            if (goalList.Count == 0) return 0.0;

            // Map each goal item to its index
            var goalIndexMap = new Dictionary<string, int>();
            for (var i = 0; i < goalList.Count; i++)
            {
                goalIndexMap[goalList[i]] = i;
            }

            var searchRanks = new List<int>();
            foreach (var item in resultList)
            {
                if (goalIndexMap.TryGetValue(item, out var idx))
                    searchRanks.Add(idx);
            }

            var n = goalList.Count;
            var totalPairs = n * (n - 1) / 2;
            if (totalPairs == 0) return 1.0; // only one item

            int concordant = 0, discordant = 0;

            for (var i = 0; i < searchRanks.Count; i++)
            {
                for (var j = i + 1; j < searchRanks.Count; j++)
                {
                    if (searchRanks[i] < searchRanks[j]) concordant++;
                    else if (searchRanks[i] > searchRanks[j]) discordant++;
                }
            }

            // For missing items, treat them as discordant
            var missingItems = n - searchRanks.Count;
            var missingPairs = missingItems * (missingItems - 1) / 2;
            discordant += (missingItems * searchRanks.Count) + missingPairs;

            var tau = (double)(concordant - discordant) / totalPairs;
            // shift from [-1..1] to [0..1]
            return (tau + 1) / 2.0;
        }

        // ------------------- 6) SortOrders -------------------
        private double EvaluateSortOrders(
            Dictionary<string, string> actualSort,
            Dictionary<string, string> expectedSort)
        {
            // Simple approach: ratio of matched props / total expected
            var actualProps = actualSort
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value) && kvp.Value != "NONE")
                .Select(kvp => $"{kvp.Key.ToLower()}:{kvp.Value.ToUpper()}")
                .ToList();

            var expectedProps = expectedSort
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value) && kvp.Value != "NONE")
                .Select(kvp => $"{kvp.Key.ToLower()}:{kvp.Value.ToUpper()}")
                .ToList();

            var matched = 0;
            foreach (var prop in expectedProps)
            {
                if (actualProps.Contains(prop)) matched++;
            }

            if (expectedProps.Count == 0)
            {
                // If nothing expected, 100% if also nothing actual
                return actualProps.Count == 0 ? 1.0 : 0.0;
            }
            return (double)matched / expectedProps.Count;
        }

        #endregion
    }
}
