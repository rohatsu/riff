// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Linq;

namespace RIFF.Core
{
    /// <summary>
    /// When Catalog Update matches the specific root key the reactor queues processing instructions
    /// for the specific processor. Depending on Date Behaviour,
    /// - when Exact: only single instruction is queued
    /// - when Latest: instructions until the next existing instance of the key in the database
    ///   (excluding that date)
    /// - when Previous: instructions until the next existing instance of the key in the database
    ///   (including that date)
    /// - when Range: instructions for processing dates derived from updates up until the next
    ///   existing instance of the key in the database
    /// </summary>
    internal class RFGraphReactor : RFEventReactor
    {
        private readonly IRFReadingContext _context;
        private readonly RFDateBehaviour _dateBehaviour;
        private readonly Func<RFGraphInstance, RFDate> _dateFunc;
        private readonly RFCatalogKey _key;
        private readonly Func<RFDate, RFDate> _maxDateFunc;
        private readonly string _processName;

        public RFGraphReactor(RFCatalogKey key, RFDateBehaviour dateBehaviour, string processName, IRFReadingContext context, Func<RFGraphInstance, RFDate> dateFunc,
            Func<RFDate, RFDate> maxDateFunc)
        {
            _key = key;
            _dateBehaviour = dateBehaviour;
            _processName = processName;
            _context = context;
            _dateFunc = dateFunc;
            _maxDateFunc = maxDateFunc;
        }

        public static RFGraphReactor RangeReactor(RFCatalogKey key, string processName, IRFReadingContext context, Func<RFGraphInstance, RFDate>
            rangeUpdateFunc, Func<RFDate, RFDate> maxInstanceFunc)
        {
            return new RFGraphReactor(key, RFDateBehaviour.Range, processName, context, rangeUpdateFunc, (d) => maxInstanceFunc(d));
        }

        public static RFGraphReactor SimpleReactor(RFCatalogKey key, RFDateBehaviour dateBehaviour, string processName, IRFReadingContext context, Func<RFDate, RFDate> maxInstanceFunc)
        {
            return new RFGraphReactor(key, dateBehaviour, processName, context, d => d.ValueDate.Value, (d) => maxInstanceFunc(d));
        }

        public override List<RFInstruction> React(RFEvent e)
        {
            var cu = e as RFCatalogUpdateEvent;
            if (cu != null && cu.Key != null && cu.Key.GraphInstance != null && cu.Key.MatchesRoot(_key))
            {
                var key = cu.Key;
                var updateDate = key.GraphInstance.ValueDate.Value;
                var processingDate = _dateFunc(key.GraphInstance); // this can be a forward date in case of a range input, but usually 1:1 with key's date
                var instructions = new List<RFInstruction>();
                if (_dateBehaviour != RFDateBehaviour.Previous)
                {
                    instructions.Add(new RFGraphProcessInstruction(cu.Key.GraphInstance.WithDate(processingDate), _processName));
                }

                // for exact inputs there's no need to queue any forward instructions
                if (_dateBehaviour != RFDateBehaviour.Exact)
                {
                    var maxDate = RFDate.NullDate;

                    // use key's original updatedate rather than derived processingdate for the instruction
                    var laterInstances = _context.GetKeyInstances(key).Where(k => k.Key.Name == key.GraphInstance.Name && k.Key.ValueDate.HasValue)
                        .Where(d => d.Key.ValueDate.Value > updateDate);
                    if (laterInstances.Any())
                    {
                        maxDate = laterInstances.Min(i => i.Key.ValueDate.Value); // next instance date
                        if (_dateBehaviour == RFDateBehaviour.Latest || _dateBehaviour == RFDateBehaviour.Range)
                        {
                            maxDate = maxDate.OffsetDays(-1); // exclude next instance date (for Previous - it will run)
                        }
                    }
                    else // there are no future instances... so how far do we go?
                    {
                        maxDate = _maxDateFunc(updateDate);
                    }

                    if (maxDate < updateDate)
                    {
                        maxDate = updateDate; // don't do anything
                    }

                    var maxInstanceDate = _maxDateFunc(_context.Today);
                    var nextDate = key.GraphInstance.ValueDate.Value.OffsetDays(1);
                    var forwardProcessingDates = new SortedSet<RFDate>(); // eliminate dupes
                    foreach (var forwardUpdateDate in RFDate.Range(nextDate, maxDate, d => true))
                    {
                        var forwardProcessingDate = _dateFunc(key.GraphInstance.WithDate(forwardUpdateDate));
                        if (forwardProcessingDate != processingDate && forwardProcessingDate <= maxInstanceDate)
                        {
                            forwardProcessingDates.Add(forwardProcessingDate);
                        }
                    }

                    if (forwardProcessingDates.Any())
                    {
                        instructions.AddRange(forwardProcessingDates.Select(d => new RFGraphProcessInstruction(key.GraphInstance.WithDate(d), _processName)));

                        _context.SystemLog.Debug(this, "Queuing {0} forward instructions for key {1} from {2} to {3}", forwardProcessingDates.Count, key.FriendlyString(), nextDate, maxDate);
                    }
                }
                return instructions;
            }
            else
            {
                return null;
            }
        }
    }
}
