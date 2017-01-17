using Common;
using Common.Runtime;
using IntegrationService.Host.Converters;
using IntegrationService.Host.Subscriptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationServiceTests
{
    [TestClass]
    public class FlatMessageConverterTests
    {
        [TestMethod]
        public void Convert_Childless()
        {
            var obj = new
            {
                Azaza = 555,
                NullableAzaza = (int?)555,
                NullableNullAzaza = (int?)null,
                Longzaza = long.MaxValue,
                NegLongzaza = long.MinValue,
                Bool = true,
                Stazaza = "44gfgfhhhhhhhhhhhhhhhhhhhhhghghghgd",
                EmptyStr = "",
                Null = (string)null,
                Guid = Guid.NewGuid()
            };
            var converter = new FlatMessageConverter();

            var schema = new RuntimeMappingSchema(new MappingSchema(new[] {
                new MappingProperty() {
                    PathName = nameof(obj.Azaza), ClrType = typeof(int).FullName, ShortName = nameof(obj.Azaza),
                    Children = MappingProperty.Childless
                },
                new MappingProperty() {
                    PathName = nameof(obj.Stazaza), ClrType = typeof(string).FullName, ShortName = nameof(obj.Stazaza),
                    Children = MappingProperty.Childless
                },
                new MappingProperty() {
                    PathName = nameof(obj.Guid), ClrType = typeof(Guid).FullName, ShortName = nameof(obj.Guid),
                    Children = MappingProperty.Childless
                },
                new MappingProperty() {
                    PathName = nameof(obj.Longzaza), ClrType = typeof(long).FullName, ShortName = nameof(obj.Longzaza),
                    Children = MappingProperty.Childless
                },
                new MappingProperty() {
                    PathName = nameof(obj.Bool), ClrType = typeof(bool).FullName, ShortName = nameof(obj.Bool),
                    Children = MappingProperty.Childless
                },
                new MappingProperty() {
                    PathName = nameof(obj.NegLongzaza), ClrType = typeof(long).FullName, ShortName = nameof(obj.NegLongzaza),
                    Children = MappingProperty.Childless
                },
                new MappingProperty() {
                    PathName = nameof(obj.Null), ClrType = typeof(string).FullName, ShortName = nameof(obj.Null),
                    Children = MappingProperty.Childless
                },
                new MappingProperty() {
                    PathName = nameof(obj.NullableAzaza), ClrType = typeof(int?).FullName, ShortName = nameof(obj.NullableAzaza),
                    Children = MappingProperty.Childless
                },
                new MappingProperty() {
                    PathName = nameof(obj.NullableNullAzaza), ClrType = typeof(int?).FullName, ShortName = nameof(obj.NullableNullAzaza),
                    Children = MappingProperty.Childless
                },
                new MappingProperty() {
                    PathName = nameof(obj.EmptyStr), ClrType = typeof(string).FullName, ShortName = nameof(obj.EmptyStr),
                    Children = MappingProperty.Childless
                }
            }, -1, DateTime.UtcNow));

            var messageFromNonArray = converter.Convert(CreateMessage(obj), schema);
            var messageFromArray = converter.Convert(new[] { CreateMessage(obj) }, schema);

            foreach (var message in new[] { messageFromArray, messageFromNonArray })
            {
                Assert.AreEqual(obj.Azaza, message.TablesWithData[MappingSchema.RootName].Single()[nameof(obj.Azaza)]);
                Assert.AreEqual(obj.Stazaza, message.TablesWithData[MappingSchema.RootName].Single()[nameof(obj.Stazaza)]);
                Assert.AreEqual(obj.Guid, message.TablesWithData[MappingSchema.RootName].Single()[nameof(obj.Guid)]);
                Assert.AreEqual(obj.Longzaza, message.TablesWithData[MappingSchema.RootName].Single()[nameof(obj.Longzaza)]);
                Assert.AreEqual(obj.Bool, message.TablesWithData[MappingSchema.RootName].Single()[nameof(obj.Bool)]);
                Assert.AreEqual(obj.NegLongzaza, message.TablesWithData[MappingSchema.RootName].Single()[nameof(obj.NegLongzaza)]);
                Assert.AreEqual(obj.NullableAzaza, message.TablesWithData[MappingSchema.RootName].Single()[nameof(obj.NullableAzaza)]);
                Assert.AreEqual(obj.EmptyStr, message.TablesWithData[MappingSchema.RootName].Single()[nameof(obj.EmptyStr)]);

                Assert.IsFalse(message.TablesWithData[MappingSchema.RootName].Single().ContainsKey(nameof(obj.Null)));
                Assert.IsFalse(message.TablesWithData[MappingSchema.RootName].Single().ContainsKey(nameof(obj.NullableNullAzaza)));
            }
        }

        [TestMethod]
        public void Convert_Childfull()
        {
            var e1 = new { V1 = 1 };
            var e2 = new { V1 = 2 };
            var obj = new { L1 = new { L2 = new { Val = 5 }, Arr = new[] { e1, e2 } } };

            var converter = new FlatMessageConverter();

            var arrPath = MappingProperty.ConcatPathName(nameof(obj.L1), nameof(obj.L1.Arr));
            var l2Path = MappingProperty.ConcatPathName(nameof(obj.L1), nameof(obj.L1.L2));

            var schema = new RuntimeMappingSchema(new MappingSchema(new[] {
                new MappingProperty()
                {
                    PathName = nameof(obj.L1), ClrType = null, ShortName = nameof(obj.L1),
                    Children = new[] {
                        new MappingProperty()
                        {
                            PathName = MappingProperty.ConcatPathName(nameof(obj.L1), nameof(obj.L1.L2)), ClrType = null, ShortName = nameof(obj.L1.L2),
                            Children = new[]
                            {
                                new MappingProperty()
                                {
                                    PathName = MappingProperty.ConcatPathName(
                                                    MappingProperty.ConcatPathName(nameof(obj.L1), nameof(obj.L1.L2)),
                                                    nameof(obj.L1.L2.Val)),
                                    ClrType = typeof(int).FullName,
                                    ShortName = nameof(obj.L1.L2.Val),
                                    Children = MappingProperty.Childless
                                }
                            }
                        },
                        new MappingProperty()
                        {
                            PathName = arrPath, ClrType = null, ShortName = nameof(obj.L1.Arr),
                            Children = new []
                            {
                                new MappingProperty()
                                {
                                    PathName = MappingProperty.ConcatPathName(arrPath, nameof(e1.V1)),
                                    ClrType = typeof(int).FullName,
                                    ShortName =  nameof(e1.V1),
                                    Children = MappingProperty.Childless
                                }
                            }
                        }
                    }
                }
            }, -1, DateTime.UtcNow));

            var message = converter.Convert(CreateMessage(obj), schema);

            Assert.AreEqual(obj.L1.Arr.Length, message.TablesWithData[arrPath].Count);
            Assert.AreEqual(obj.L1.Arr.Max(e => e.V1), message.TablesWithData[arrPath].Max(e => (int)e[nameof(e1.V1)]));
            Assert.AreEqual(obj.L1.Arr.Min(e => e.V1), message.TablesWithData[arrPath].Min(e => (int)e[nameof(e1.V1)]));

            Assert.AreEqual(obj.L1.L2.Val, message.TablesWithData[l2Path].Single()[nameof(obj.L1.L2.Val)]);
        }

        private RawMessage CreateMessage(object o)
        {
            if (o.GetType().IsArray)
            {
                return new RawMessage(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(o)), ((Array)o).Length);
            }
            {
                return new RawMessage(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(o)), 1);
            }
        }
    }
}
