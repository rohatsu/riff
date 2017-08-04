// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    /// <summary>
    /// Legacy - use domain-templated RFGraphProcessDefinition
    /// </summary>
    public class RFGraphProcessDefinition
    {
        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string GraphName { get; set; }

        [DataMember]
        public List<RFGraphIOMapping> IOMappings { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public Func<IRFGraphProcessorInstance> Processor { get; set; }

        public RFGraphProcessDefinition MapInput<S>(Expression<Func<S, object>> property, RFCatalogKey key) where S : RFGraphProcessorDomain
        {
            return AddIOMapping(property, key, RFIOBehaviour.Input);
        }

        public RFGraphProcessDefinition MapOutput<S>(Expression<Func<S, object>> property, RFCatalogKey key) where S : RFGraphProcessorDomain
        {
            return AddIOMapping(property, key, RFIOBehaviour.Output);
        }

        public RFGraphProcessDefinition MapRangeInput<S>(Expression<Func<S, object>> property, RFCatalogKey key, Func<RFGraphInstance, List<RFDate>> rangeRequestFunc,
                    Func<RFGraphInstance, RFDate> rangeUpdateFunc) where S : RFGraphProcessorDomain
        {
            return AddIOMapping(property, key, RFIOBehaviour.Input, rangeRequestFunc, rangeUpdateFunc);
        }

        public RFGraphProcessDefinition MapState<S>(Expression<Func<S, object>> property, RFCatalogKey key) where S : RFGraphProcessorDomain
        {
            return AddIOMapping(property, key, RFIOBehaviour.State);
        }

        protected RFGraphProcessDefinition AddIOMapping<S>(Expression<Func<S, object>> property, RFCatalogKey key, RFIOBehaviour expectedBehvaiour, Func<RFGraphInstance, List<RFDate>> rangeRequestFunc = null,
            Func<RFGraphInstance, RFDate> rangeUpdateFunc = null) where S : RFGraphProcessorDomain
        {
            var processor = Processor();
            var domain = processor.CreateDomain();
            if (domain.GetType() != typeof(S))
            {
                throw new RFLogicException(this, "IO Domain Type mismatch on processor {0}: {1} vs {2}", RFGraphDefinition.GetFullName(GraphName, Name),
                    domain.GetType().Name, typeof(S).Name);
            }

            var propertyInfo = property.GetProperty<S>();
            var ioBehaviour = RFReflectionHelpers.GetIOBehaviour(propertyInfo);
            if (ioBehaviour != expectedBehvaiour)
            {
                throw new RFLogicException(this, "Inconsistent IO direction {0} on property {1}, expected {2}.", ioBehaviour, propertyInfo.FullName(), expectedBehvaiour);
            }
            var dateBehaviour = RFReflectionHelpers.GetDateBehaviour(propertyInfo);
            if (dateBehaviour == RFDateBehaviour.Range && (rangeRequestFunc == null || rangeUpdateFunc == null))
            {
                throw new RFLogicException(this, "Range input doesn't have range functions specified on property {0}.", propertyInfo.FullName());
            }
            IOMappings.Add(new RFGraphIOMapping { Key = key, Property = propertyInfo, RangeRequestFunc = rangeRequestFunc, RangeUpdateFunc = rangeUpdateFunc, DateBehaviour = dateBehaviour });
            return this;
        }
    }

    /// <summary>
    /// Class representing IO configuration of a Graph Processor.
    /// </summary>
    public class RFGraphProcessDefinition<D> : RFGraphProcessDefinition where D : RFGraphProcessorDomain
    {
        /// <summary>
        /// Link a catalog entry to domain's property.
        /// </summary>
        /// <param name="property">Property selector</param>
        /// <param name="key">Catalog Key root to be linked (no instance required)</param>
        /// <param name="dateBehaviour">Date loading behaviour</param>
        /// <returns></returns>
        public RFGraphProcessDefinition<D> Map(Expression<Func<D, object>> property, RFCatalogKey key, RFDateBehaviour dateBehaviour)
        {
            // auto-derive type
            var processor = Processor();
            var domain = processor.CreateDomain();
            if (domain.GetType() != typeof(D))
            {
                throw new RFLogicException(this, "IO Domain Type mismatch on processor {0}: {1} vs {2}", RFGraphDefinition.GetFullName(GraphName, Name),
                    domain.GetType().Name, typeof(D).Name);
            }

            var propertyInfo = property.GetProperty<D>();
            var ioBehaviour = RFReflectionHelpers.GetIOBehaviour(propertyInfo);
            var declaredDateBehaviour = RFReflectionHelpers.GetDateBehaviour(propertyInfo);
            if (declaredDateBehaviour != RFDateBehaviour.NotSet && declaredDateBehaviour != dateBehaviour)
            {
                throw new RFLogicException(this, "DateBehaviour mismatch on processor {0}: {1} vs {2}", RFGraphDefinition.GetFullName(GraphName, Name),
                    declaredDateBehaviour, dateBehaviour);
            }
            if (dateBehaviour == RFDateBehaviour.Range)
            {
                throw new RFLogicException(this, "Use MapRange to define ranged IO on property {0}.", propertyInfo.FullName());
            }
            IOMappings.Add(new RFGraphIOMapping { Key = key, Property = propertyInfo, RangeRequestFunc = null, RangeUpdateFunc = null, DateBehaviour = dateBehaviour });
            return this;
        }

        /// <summary>
        /// Link a range of catalog entries to domain's property.
        /// </summary>
        /// <param name="property">Property selector</param>
        /// <param name="key">Catalog Key root to be linked (no instance required)</param>
        /// <param name="rangeRequestFunc">Defines input keys' date range based on RFGraphInstance being processed</param>
        /// <param name="rangeUpdateFunc">For an updated key's ValueDate determines the RFGraphInstance to run the processor for (inverse of the rangeRequestFunc)</param>
        /// <returns></returns>
        public RFGraphProcessDefinition<D> MapRange(Expression<Func<D, object>> property, RFCatalogKey key, Func<RFGraphInstance,
            List<RFDate>> rangeRequestFunc,
            Func<RFGraphInstance, RFDate> rangeUpdateFunc)
        {
            // auto-derive type
            var processor = Processor();
            var domain = processor.CreateDomain();
            if (domain.GetType() != typeof(D))
            {
                throw new RFLogicException(this, "IO Domain Type mismatch on processor {0}: {1} vs {2}", RFGraphDefinition.GetFullName(GraphName, Name),
                    domain.GetType().Name, typeof(D).Name);
            }

            var propertyInfo = property.GetProperty<D>();
            var ioBehaviour = RFReflectionHelpers.GetIOBehaviour(propertyInfo);
            var declaredDateBehaviour = RFReflectionHelpers.GetDateBehaviour(propertyInfo);
            if (declaredDateBehaviour != RFDateBehaviour.NotSet && declaredDateBehaviour != RFDateBehaviour.Range)
            {
                throw new RFLogicException(this, "DateBehaviour mismatch on processor {0}: {1} vs {2}", RFGraphDefinition.GetFullName(GraphName, Name),
                    declaredDateBehaviour, RFDateBehaviour.Range);
            }
            IOMappings.Add(new RFGraphIOMapping { Key = key, Property = propertyInfo, RangeRequestFunc = rangeRequestFunc, RangeUpdateFunc = rangeUpdateFunc, DateBehaviour = RFDateBehaviour.Range });
            return this;
        }
    }
}
