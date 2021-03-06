﻿using FakeItEasy;
using FakeXrmEasy;
using Microsoft.Xrm.Sdk;
using System;
using System.IO;
using Xunit;

namespace Xrm.Oss.UnitOfWork.Tests
{
    public class UpdateContextTests
    {
        [Fact]
        public void Should_Keep_Logical_Name_And_Id()
        {
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid()
            };

            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                contact["firstname"] = "Frodo";

                var update = updateContext.GetUpdateObject();

                Assert.Equal(contact.LogicalName, update.LogicalName);
                Assert.Equal(contact.Id, update.Id);
            }
        }

        [Fact]
        public void Should_Add_Newly_Added_Attributes()
        {
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid()
            };

            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                contact["firstname"] = "Frodo";

                var update = updateContext.GetUpdateObject();

                Assert.True(update.Contains("firstname"));
            }
        }

        [Fact]
        public void Should_Not_Add_Unchanged_Attributes()
        {
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "lastname", "Baggins" }
                }
            };
            
            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                contact["firstname"] = "Frodo";

                var update = updateContext.GetUpdateObject();

                Assert.Equal("Frodo", update["firstname"]);
                Assert.False(update.Contains("lastname"));
            }
        }

        [Fact]
        public void Should_Return_Null_If_Nothing_Changed()
        {
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid()
            };

            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                var update = updateContext.GetUpdateObject();

                Assert.Null(update);
            }
        }

        [Fact]
        public void Should_Include_Attributes_That_Were_Set_Null()
        {
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes =
                {
                    { "lastname", "Baggins" }
                }
            };

            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                contact["lastname"] = null;

                var update = updateContext.GetUpdateObject();

                Assert.Null(update["lastname"]);
            }
        }

        [Fact]
        public void Should_Return_UpdateRequest_With_Proper_Target()
        {
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid()
            };

            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                contact["firstname"] = "Frodo";

                var updateRequest = updateContext.GetUpdateRequest();

                Assert.Equal("Frodo", updateRequest.Target["firstname"]);
            }
        }

        [Fact]
        public void Should_Send_Update_If_Changes_Made()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();

            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid()
            };

            context.Initialize(new[]{ contact });
            
            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                contact["firstname"] = "Frodo";

                var updateSent = updateContext.Update(service);

                Assert.True(updateSent);
                A.CallTo(() => service.Update(A<Entity>._)).MustHaveHappened(Repeated.Exactly.Once);
            }
        }

        [Fact]
        public void Should_Not_Send_Update_If_No_Changes_Made()
        {
            var context = new XrmFakedContext();
            var service = context.GetFakedOrganizationService();
            
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid()
            };
            
            context.Initialize(new[]{ contact });

            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                var updateSent = updateContext.Update(service);

                Assert.False(updateSent);
                A.CallTo(() => service.Update(A<Entity>._)).MustNotHaveHappened();
            }
        }
        
        [Fact]
        public void Should_Update_If_Reference_Type_Value_Changed()
        {
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "revenue", new Money(1000m) }
                }
            };

            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                ((Money) contact["revenue"]).Value = 2000m;

                var update = updateContext.GetUpdateObject();

                Assert.Equal(new Money(2000m), update["revenue"]);
            }
        }
        
        [Fact]
        public void Should_Update_If_Reference_Type_Changed()
        {
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "revenue", new Money(1000m) }
                }
            };

            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                contact["revenue"] = new Money(2000m);

                var update = updateContext.GetUpdateObject();

                Assert.Equal(new Money(2000m), update["revenue"]);
            }
        }
        
        [Fact]
        public void Should_Not_Update_If_Reference_But_Not_Value_Changed()
        {
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "revenue", new Money(1000m) }
                }
            };

            using (var updateContext = new UpdateContext<Entity>(contact))
            {
                contact["revenue"] = new Money(1000m);

                var update = updateContext.GetUpdateObject();

                Assert.Null(update);
            }
        }

        [Fact]
        public void Should_Throw_Invalid_Operation_Exception_On_Initialization_With_Null()
        {
            Assert.Throws<InvalidOperationException>(() => new UpdateContext<Entity>(null));
        }

        [Fact]
        public void Should_Throw_Exception_While_Cloning_Unknown_Type()
        {
            var contact = new Entity
            {
                LogicalName = "contact",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "fooBar", new OrganizationRequest() }
                }
            };
            
            Assert.Throws<InvalidDataException>(() => new UpdateContext<Entity>(contact));
        }

        [Fact]
        public void Update_Objects_Not_Be_Changed_By_Later_Updates_To_Original_Object()
        {
            var opportunity = new Entity
            {
                LogicalName = "opportunity",
                Id = Guid.NewGuid(),
                Attributes = new AttributeCollection
                {
                    { "activestageid", new Guid("28595D66-C790-4DD9-B06C-C6CE9BA08A6A") },
                    { "oss_testoptionset", new OptionSetValue(0) }
                }
            };

            var firstUpdateId = new Guid("39595D66-C790-4DD9-B06C-C6CE9BA08A6A");
            var secondUpdateId = new Guid("40595D66-C790-4DD9-B06C-C6CE9BA08A6A");
            var firstOptionValue = 1;
            var secondOptionValue = 2;

            using (var updateContext = new UpdateContext<Entity>(opportunity))
            {
                opportunity["activestageid"] = firstUpdateId;
                ((OptionSetValue)opportunity["oss_testoptionset"]).Value = firstOptionValue;
                var nextStageUpdate = updateContext.GetUpdateObject();

                opportunity["activestageid"] = secondUpdateId;
                ((OptionSetValue)opportunity["oss_testoptionset"]).Value = secondOptionValue;
                var stageAfterNextUpdate = updateContext.GetUpdateObject();

                Assert.Equal(firstUpdateId, nextStageUpdate["activestageid"]);
                Assert.Equal(firstOptionValue, nextStageUpdate.GetAttributeValue<OptionSetValue>("oss_testoptionset").Value);

                Assert.Equal(secondUpdateId, stageAfterNextUpdate["activestageid"]);
                Assert.Equal(secondOptionValue, stageAfterNextUpdate.GetAttributeValue<OptionSetValue>("oss_testoptionset").Value);
            }
        }
    }
}
