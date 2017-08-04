// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    [DataContract]
    public abstract class RFDataMatrix<K1, K2, C> : IRFDataSet where K1 : class, IComparable where K2 : class, IComparable
    {
        public class RFDataCell : IRFDataRow
        {
            [DataMember]
            public C C { get; set; }

            [DataMember]
            public K1 K1 { get; set; }

            [DataMember]
            public K2 K2 { get; set; }
        }

        [IgnoreDataMember]
        public Dictionary<Tuple<int, int>, C> Cells { get; set; }

        [DataMember]
        public C[][] CellsArray
        {
            get
            {
                if (Keys1.First.Any() && Keys2.First.Any())
                {
                    int l1 = Keys1.First.Max() + 1;
                    int l2 = Keys2.First.Max() + 1;
                    var c = new C[l1][];
                    for (int i = 0; i < l1; i++)
                    {
                        c[i] = new C[l2];
                        for (int j = 0; j < l2; j++)
                        {
                            var tuple = new Tuple<int, int>(i, j);
                            C v = default(C);
                            if (Cells.TryGetValue(tuple, out v))
                            {
                                c[i][j] = v;
                            }
                        }
                    }
                    return c;
                }
                else
                {
                    return new C[0][];
                }
            }

            set
            {
                Cells = new Dictionary<Tuple<int, int>, C>();
                for (int i = 0; i < value.Length; i++)
                {
                    for (int j = 0; j < value[i].Length; j++)
                    {
                        Cells.Add(new Tuple<int, int>(i, j), value[i][j]);
                    }
                }
            }
        }

        [IgnoreDataMember]
        public BiDictionary<int, K1> Keys1 { get; set; }

        // serialize as arrays
        [DataMember]
        public K1[] Keys1Array
        {
            get
            {
                if (Keys1.First.Any())
                {
                    var arr = new K1[Keys1.First.Max() + 1];
                    for (int i = 0; i <= Keys1.First.Max(); i++)
                    {
                        arr[i] = Keys1.ContainsFirst(i) ? Keys1.GetByFirst(i) : null;
                    }
                    return arr;
                }
                else
                {
                    return new K1[0];
                }
            }
            set
            {
                Keys1 = new BiDictionary<int, K1>();
                for (int i = 0; i < value.Length; i++)
                {
                    Keys1.Add(i, value[i]);
                }
            }
        }

        [IgnoreDataMember]
        public BiDictionary<int, K2> Keys2 { get; set; }

        [DataMember]
        public K2[] Keys2Array
        {
            get
            {
                if (Keys2.First.Any())
                {
                    var arr = new K2[Keys2.First.Max() + 1];
                    for (int i = 0; i <= Keys2.First.Max(); i++)
                    {
                        arr[i] = Keys2.ContainsFirst(i) ? Keys2.GetByFirst(i) : null;
                    }
                    return arr;
                }
                else
                {
                    return new K2[0];
                }
            }
            set
            {
                Keys2 = new BiDictionary<int, K2>();
                for (int i = 0; i < value.Length; i++)
                {
                    Keys2.Add(i, value[i]);
                }
            }
        }

        protected RFDataMatrix()
        {
            Keys1 = new BiDictionary<int, K1>();
            Keys2 = new BiDictionary<int, K2>();
            Cells = new Dictionary<Tuple<int, int>, C>();
        }

        public C GetCell(K1 k1, K2 k2)
        {
            if (Keys1.ContainsSecond(k1) && Keys2.ContainsSecond(k2))
            {
                var i1 = Keys1.GetBySecond(k1);
                var i2 = Keys2.GetBySecond(k2);
                var tuple = new Tuple<int, int>(i1, i2);
                if (Cells.ContainsKey(tuple))
                {
                    return Cells[tuple];
                }
            }
            return default(C);
        }

        public IEnumerable<RFDataCell> GetCells()
        {
            return Cells.Select(c => new RFDataCell
            {
                K1 = Keys1.GetByFirst(c.Key.Item1),
                K2 = Keys2.GetByFirst(c.Key.Item2),
                C = c.Value
            });
        }

        public IEnumerable<IRFDataRow> GetRows()
        {
            return GetCells();
        }

        public Type GetRowType()
        {
            return typeof(RFDataCell);
        }

        public void PutCell(K1 k1, K2 k2, C c)
        {
            int i1 = 0;
            if (!Keys1.TryGetBySecond(k1, out i1))
            {
                int new1 = Keys1.First.Any() ? Keys1.First.Max() + 1 : 0;
                Keys1.Add(new1, k1);
                i1 = new1;
            }

            int i2 = 0;
            if (!Keys2.TryGetBySecond(k2, out i2))
            {
                int new2 = Keys2.First.Any() ? Keys2.First.Max() + 1 : 0;
                Keys2.Add(new2, k2);
                i2 = new2;
            }

            var tuple = new Tuple<int, int>(i1, i2);
            Cells.Remove(tuple);
            Cells.Add(tuple, c);
        }
    }
}
