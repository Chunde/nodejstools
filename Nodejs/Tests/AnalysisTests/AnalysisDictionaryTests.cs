﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.NodejsTools.Analysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;

namespace NodejsTests {
    [TestClass]
    public class AnalysisDictionaryTests {
        [TestMethod, Priority(0)]
        public void TestBasic() {
            var dict = new AnalysisDictionary<string, string>();
            Assert.IsFalse(dict.Remove("Foo"));
            dict.Add("1", "One");
            dict.Add("2", "Two");
            dict.Add("3", "Three");

            Assert.AreEqual("One", dict["1"]);
            Assert.AreEqual("Two", dict["2"]);
            Assert.AreEqual("Three", dict["3"]);
            Assert.IsTrue(dict.ContainsKey("1"));
            Assert.IsTrue(dict.ContainsKey("2"));
            Assert.IsTrue(dict.ContainsKey("3"));

            var keys = dict.ToArray().Select(x => x.Key).ToArray();
            AssertUtil.ContainsExactly(keys, "1", "2", "3");
            AssertUtil.ContainsExactly(dict.Keys, "1", "2", "3");
            AssertUtil.ContainsExactly(dict.KeysNoCopy, "1", "2", "3");
            AssertUtil.ContainsExactly(dict.Values, "One", "Two", "Three");
            AssertUtil.ContainsExactly(dict.EnumerateValues, "One", "Two", "Three");

            Assert.IsTrue(dict.Contains(new KeyValuePair<string, string>("1", "One")));
            Assert.IsFalse(dict.Contains(new KeyValuePair<string, string>("1", "AAA")));

            var items = new KeyValuePair<string, string>[3];
            dict.CopyTo(items, 0);
            AssertUtil.ContainsExactly(
                items,
                new KeyValuePair<string, string>("1", "One"),
                new KeyValuePair<string, string>("2", "Two"),
                new KeyValuePair<string, string>("3", "Three")
            );

            Assert.IsFalse(dict.IsReadOnly);
            Assert.AreEqual(EqualityComparer<string>.Default, dict.Comparer);

            Assert.AreEqual(dict.Count, 3);

            dict.Remove("1");

            Assert.AreEqual(dict.Count, 2);

            string value;
            Assert.AreEqual(false, dict.TryGetValue("1", out value));
            Assert.IsFalse(dict.ContainsKey("1"));
            Assert.AreEqual(null, value);

            dict["1"] = "One";

            Assert.AreEqual(dict.Count, 3);
            Assert.AreEqual("One", dict["1"]);
            Assert.AreEqual("Two", dict["2"]);
            Assert.AreEqual("Three", dict["3"]);
            Assert.IsTrue(dict.ContainsKey("1"));
            Assert.IsTrue(dict.ContainsKey("2"));
            Assert.IsTrue(dict.ContainsKey("3"));

            dict.Clear();
            Assert.IsFalse(dict.Remove("Foo"));
            Assert.AreEqual(0, dict.Count);
            Assert.IsFalse(dict.ContainsKey("1"));
            Assert.IsFalse(dict.ContainsKey("2"));
            Assert.IsFalse(dict.ContainsKey("3"));

            dict.Add(new KeyValuePair<string, string>("1", "One"));
            dict.Add(new KeyValuePair<string, string>("2", "Two"));
            dict.Add(new KeyValuePair<string, string>("3", "Three"));

            Assert.IsTrue(dict.Remove(new KeyValuePair<string, string>("1", "One")));
            Assert.IsFalse(dict.Remove(new KeyValuePair<string, string>("1", "One")));

            Assert.IsFalse(dict.Remove(new KeyValuePair<string, string>("2", "AAA")));
            Assert.IsTrue(dict.Remove(new KeyValuePair<string, string>("2", "Two")));
            Assert.IsTrue(dict.Remove(new KeyValuePair<string, string>("3", "Three")));

            Assert.AreEqual(0, dict.Count);

            AssertUtil.Throws<KeyNotFoundException>(
                () => { var x = dict["DNE"]; }
            );
        }

        [TestMethod, Priority(0)]
        public void TestAddExisting() {
            var dict = new AnalysisDictionary<string, string>() { { "1", "One" } };
            AssertUtil.Throws<ArgumentException>(() => dict.Add("1", "Two"));
        }

        [TestMethod, Priority(0)]
        public void TestGrowth() {
            var dict = new AnalysisDictionary<string, string>();
            for (int i = 0; i < 25; i++) {
                dict[i.ToString()] = i.ToString();

                for (int j = 0; j < i; j++) {
                    Assert.AreEqual(j.ToString(), dict[j.ToString()]);
                }
            }

            dict = new AnalysisDictionary<string, string>(15);
            for (int i = 0; i < 25; i++) {
                dict[i.ToString()] = i.ToString();

                for (int j = 0; j < i; j++) {
                    Assert.AreEqual(j.ToString(), dict[j.ToString()]);
                }
            }
        }

        [TestMethod, Priority(0)]
        public void TestReplaceExisting() {
            // same object reference
            var dict = new AnalysisDictionary<string, string>();
            var value = "1";
            dict.Add(value, "One");
            dict[value] = "Two";
            Assert.AreEqual(1, dict.Count);
            Assert.AreEqual("Two", dict["1"]);

            // difference object reference, but equal.
            var dict2 = new AnalysisDictionary<Hashable, string>();
            var one = new Hashable(1, (self, x) => x._hash == 1);
            var two = new Hashable(1, (self, x) => x._hash == 1);
            dict2.Add(one, "One");
            dict2[two] = "Two";

            Assert.AreEqual("Two", dict2[one]);
            Assert.AreEqual("Two", dict2[two]);

            Assert.AreEqual(1, dict2.Count);
        }

        [TestMethod, Priority(0)]
        public void TestRemoveExisting() {
            var dict = new AnalysisDictionary<Hashable, string>();
            var one = new Hashable(1, (self, x) => x._hash == 1);
            var two = new Hashable(1, (self, x) => x._hash == 1);
            dict.Add(one, "One");
            dict.Remove(two);
            Assert.AreEqual(0, dict.Count);
        }

        [TestMethod, Priority(0)]
        public void TestNull() {
            var dict = new AnalysisDictionary<string, string>();
            string dummy;
            AssertUtil.Throws<ArgumentNullException>(
                () => dict.TryGetValue(null, out dummy)
            );
            AssertUtil.Throws<ArgumentNullException>(
                () => dict.Remove(null)
            );
            AssertUtil.Throws<ArgumentNullException>(
                () => dict.Add(null, "")
            );
            AssertUtil.Throws<ArgumentNullException>(
                () => dict.ContainsKey(null)
            );
        }

        [TestMethod, Priority(0)]
        public void TestAddRemoveAdd() {
            // same object reference
            var dict = new AnalysisDictionary<string, string>();
            var value = "1";
            dict.Add(value, "One");
            dict[value] = "Two";
            Assert.AreEqual(1, dict.Count);
            Assert.AreEqual("Two", dict["1"]);

            // difference object reference, but equal.
            var dict2 = new AnalysisDictionary<Hashable, string>();
            var one = new Hashable(1, (self, x) => x._hash == 1);
            var two = new Hashable(1, (self, x) => x._hash == 1);
            dict2.Add(one, "One");
            dict2[two] = "Two";

            Assert.AreEqual("Two", dict2[one]);
            Assert.AreEqual("Two", dict2[two]);

            Assert.AreEqual(1, dict2.Count);

            dict2 = new AnalysisDictionary<Hashable, string>();
            dict2.Add(new Hashable(1), "One");
            Assert.AreEqual(false, dict2.Remove(new Hashable(2)));
        }

        [TestMethod, Priority(0)]
        public void TestCollision() {
            // difference object reference, but equal.
            var dict = new AnalysisDictionary<Hashable, string>();
            List<Hashable> items = new List<Hashable>();
            for (int i = 0; i < 25; i++) {
                var item = new Hashable(1);
                items.Add(item);
                dict[item] = i.ToString();
            }

            for (int i = 0; i < items.Count; i++) {
                Assert.AreEqual(i.ToString(), dict[items[i]]);
            }

            for (int i = 0; i < items.Count; i++) {
                dict.Remove(items[i]);

                for (int j = i + 1; j < items.Count; j++) {
                    Assert.AreEqual(j.ToString(), dict[items[j]]);
                }
            }
        }

        [TestMethod, Priority(0)]
        public void TestComparer() {
            // difference object reference, but equal.
            foreach (var dict in new[] {
                new AnalysisDictionary<string, string>(MyComparer.Instance),
                new AnalysisDictionary<string, string>(10, MyComparer.Instance)
            }) {
                dict.Add("X1", "One");
                dict.Add("X2", "Two");
                Assert.AreEqual("One", dict["Y1"]);
                Assert.AreEqual("Two", dict["Y2"]);
            }
        }

        [TestMethod, Priority(0)]
        public void TestEnumeration() {
            var dict = new AnalysisDictionary<string, string>() { { "1", "One" } };
            var enumer = dict.GetEnumerator();
            while (enumer.MoveNext()) {
                Assert.AreEqual("1", enumer.Current.Key);
            }

            var enumer2 = ((IEnumerable)dict).GetEnumerator();
            while (enumer2.MoveNext()) {
                Assert.AreEqual("1", ((KeyValuePair<string, string>)enumer2.Current).Key);
            }
        }

        [TestMethod, Priority(0)]
        public void TestClear() {
            var dict = new AnalysisDictionary<string, string>();
            dict.Clear();
            Assert.AreEqual(0, dict.Count);

            dict.Add("Foo", "Foo");
            Assert.AreEqual(1, dict.Count);

            dict.Clear();
            Assert.AreEqual(0, dict.Count);

            dict.Clear();
            Assert.AreEqual(0, dict.Count);
        }

        /// <summary>
        /// Tests the racing code path in AnalysisDictionary.TryGetValue
        /// 
        /// Tests when the value is removed and replaced with an equivalent value 
        /// that we pick up the new value correctly.
        /// </summary>
        [TestMethod, Priority(0)]
        public void TestThreadedReaderUpdatedValue() {
            var comparer = new SynchronizedComparer();

            var dict = new AnalysisDictionary<Hashable, string>(comparer);
            var key = new Hashable(1, HashEquals);
            dict[key] = "0";
            var thread = new Thread(() => {
                comparer.EqualWaiting.WaitOne();
                dict.Remove(key);
                dict[new Hashable(1, HashEquals)] = "1";
                comparer.DictUpdated.Set();
            });
            thread.Start();

            Assert.AreEqual("1", dict[new Hashable(1, HashEquals)]);
        }

        /// <summary>
        /// Tests the racing code path in AnalysisDictionary.TryGetValue
        /// 
        /// Tests when the value is removed and replaced with a non-equivalent
        /// value that we don't find the non-equivalent value.
        /// </summary>
        [TestMethod, Priority(0)]
        public void TestThreadedReaderValueRemoved() {
            var comparer = new SynchronizedComparer();

            var dict = new AnalysisDictionary<Hashable, string>(comparer);
            var key = new Hashable(1, HashEquals);
            dict[key] = "0";
            var thread = new Thread(() => {
                comparer.EqualWaiting.WaitOne();
                dict.Remove(key);
                dict[new Hashable(1)] = "1";
                comparer.DictUpdated.Set();
            });
            thread.Start();

            Assert.IsFalse(dict.ContainsKey(new Hashable(1, HashEquals)));
        }

        /// <summary>
        /// Tests concurrent writer with concurrent reader
        /// </summary>
        [TestMethod, Priority(2)]
        public void TestThreaded() {
            var dict = new AnalysisDictionary<string, string>();
            var keys = new[] { "1", "2", "3", "4", "5", "6" };
            foreach (var key in keys) {
                dict[key] = "0";
            }
            bool exit = false;
            var thread = new Thread(() => {
                int count = 1;
                while (!exit) {
                    var countStr = count.ToString();
                    foreach (var key in keys) {
                        dict[key] = countStr;
                    }
                    count++;
                }
            });
            thread.Start();

            Dictionary<string, int> lastKey = new Dictionary<string, int>();
            foreach (var key in keys) {
                lastKey[key] = 0;
            }
            for (int i = 0; i < 10000000; i++) {
                foreach (var key in keys) {
                    var value = Int32.Parse(dict[key]);
                    Assert.IsTrue(value >= lastKey[key]);
                    lastKey[key] = value;
                }

                foreach (var kvp in dict) {
                    var value = Int32.Parse(kvp.Value);
                    Assert.IsTrue(value >= lastKey[kvp.Key]);
                    lastKey[kvp.Key] = value;
                }
            }
            exit = true;
        }


        private static bool HashEquals(Hashable x, Hashable y) {
            return x._hash == y._hash &&
                x._equals == y._equals;
        }

        class SynchronizedComparer : IEqualityComparer<Hashable> {
            public readonly AutoResetEvent DictUpdated = new AutoResetEvent(false);
            public readonly AutoResetEvent EqualWaiting = new AutoResetEvent(false);
            private bool _equalCalled;

            public bool Equals(Hashable x, Hashable y) {
                if (!_equalCalled) {
                    _equalCalled = true;
                    EqualWaiting.Set();
                    DictUpdated.WaitOne();
                }
                return x.Equals(y);
            }

            public int GetHashCode(Hashable obj) {
                return obj.GetHashCode();
            }
        }

        class MyComparer : IEqualityComparer<string> {
            public static MyComparer Instance = new MyComparer();

            public bool Equals(string x, string y) {
                return x.Substring(1) == y.Substring(1);
            }

            public int GetHashCode(string obj) {
                return obj.Substring(1).GetHashCode();
            }
        }

        class Hashable {
            public readonly int _hash;
            public readonly Func<Hashable, Hashable, bool> _equals;

            public Hashable(int hash, Func<Hashable, Hashable, bool> equals = null) {
                _hash = hash;
                _equals = equals ?? Object.ReferenceEquals;
            }

            public override int GetHashCode() {
                return _hash;
            }

            public override bool Equals(object obj) {

                return _equals(this, (Hashable)obj);
            }
        }
    }
}