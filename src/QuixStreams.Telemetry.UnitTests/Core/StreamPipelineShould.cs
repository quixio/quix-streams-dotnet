using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using QuixStreams.Telemetry.Models;
using QuixStreams.Telemetry.UnitTests.Helpers;
using Xunit;

namespace QuixStreams.Telemetry.UnitTests
{
    public class StreamPipelineShould
    {
        [Fact]
        public void Send_AfterAddComponent_ShouldExecuteSubscribersHandlers()
        {
            // Arrange
            IStreamPipeline pipeline = new StreamPipeline();
            StreamComponent component = new StreamComponent();
            component.Input.LinkTo(component.Output);
            pipeline.AddComponent(component);

            TestModel1 testModel1 = new TestModel1();
            TestModel1 handledModel1 = null;

            TestModel2 testModel2 = new TestModel2();
            TestModel2 handledModel2 = null;

            int num = 0;
            pipeline.Subscribe<TestModel1>((stream, test) =>
            {
                handledModel1 = test;
                num++;
            });

            // Act
            pipeline.Send(testModel1);
            pipeline.Send(testModel2);

            // Assert
            Assert.Equal(testModel1, handledModel1);
            Assert.Null(handledModel2);
            Assert.Equal(1, num);
        }
        
        [Theory]
        [InlineData("valid-stream-id", false)]
        [InlineData("also/valid/stream/id", false)]
        [InlineData("also\\valid\\stream\\id", false)]
        public void Constructor_StreamId_ShouldDoExpected(string streamId, bool throwArgumentOutOfRangeException)
        {
            Action action = () =>  new StreamPipeline(streamId);
            if (throwArgumentOutOfRangeException)
            {
                action.Should().Throw<ArgumentOutOfRangeException>();
            }
            else
            {
                action.Should().NotThrow<ArgumentOutOfRangeException>();
            }
        }


        [Fact]
        public void Send_WithPackageSubscription_ShouldExecutePackageSubscribersHandlers()
        {
            // Arrange
            IStreamPipeline pipeline = new StreamPipeline();
            StreamComponent component = new StreamComponent();
            component.Input.LinkTo(component.Output);
            pipeline.AddComponent(component);

            TestModel1 testModel1 = new TestModel1();
            TestModel1 handledModel1 = null;

            TestModel2 testModel2 = new TestModel2();

            StreamPackage handledPackage = null;

            pipeline.Subscribe((stream, test) =>
            {
                handledPackage = test;
            });

            pipeline.Subscribe<TestModel1>((stream, test) =>
            {
                handledModel1 = test;
            });

            // Act
            pipeline.Send(new StreamPackage(typeof(TestModel1), testModel1));

            // Assert
            Assert.Equal(testModel1, handledModel1);
            Assert.Equal(testModel1, handledPackage.Value);

            // Act
            pipeline.Send(new StreamPackage(typeof(TestModel2), testModel2));

            // Assert
            Assert.Equal(testModel1, handledModel1);
            Assert.Equal(testModel2, handledPackage.Value);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Send_WithMultipleComponentRegistered_ReceiveExpected(int numberOfComponents)
        {
            // Arrange
            var invokingComponents = new List<BypassComponent<TestModel1>>();
            IStreamPipeline pipeline = new StreamPipeline();
            TestModel1 testModel1 = new TestModel1();
            
            void Callback(BypassComponent<TestModel1> component, TestModel1 package)
            {
                if (package != testModel1) throw new Exception("Incorrect package raised");
                invokingComponents.Add(component);
            }
            
            var components = new List<BypassComponent<TestModel1>>();
            for (int i = 0; i < numberOfComponents; i++)
            {
                var component = new BypassComponent<TestModel1>(Callback);
                components.Add(component);
                pipeline.AddComponent(component);
            }
            

            // Act
            pipeline.Send(testModel1);

            // Assert
            invokingComponents.Select(y=> y.Id)
                .Should().BeEquivalentTo(components.Select(y=> y.Id), o => o.WithStrictOrdering());
        }

        private class BypassComponent<T> : StreamComponent
        {

            public readonly Guid Id = Guid.NewGuid();
            
            public BypassComponent(Action<BypassComponent<T>, T> callback)
            {
                this.Input.Subscribe<T>(package =>
                {
                    callback(this, package);
                });
                this.Input.LinkTo(this.Output);
            }
        }

    }

}


