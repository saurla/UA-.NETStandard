using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Opc.Ua.Core.Tests.Stack.State
{
    /// <summary>
    /// Tests for concurrency issues in BaseVariableState class
    /// </summary>
    [TestFixture]
    [SetCulture("en-us"), SetUICulture("en-us")]
    [Category("NodeStateConcurrency")]
    [Parallelizable]
    public class NodeStateCollectionConcurrencyTests
    {
        [Test]
        public void NodeStateReferencesCollectionConcurrencyTest()
        {
            var testNodeState = new AnalogUnitRangeState(null);
            var serviceMessageContext = new ServiceMessageContext();

            var systemContext = new SystemContext() { NamespaceUris = serviceMessageContext.NamespaceUris };

            testNodeState.Create(
                new SystemContext() { NamespaceUris = serviceMessageContext.NamespaceUris },
                new NodeId("TestNode", (ushort)7),
                new QualifiedName("TestNode", (ushort)7),
                new LocalizedText("TestNode"),
                true);

            List<IReference> references = new List<IReference>();

            testNodeState.GetReferences(systemContext, references);

            int originalReferenceCount = references.Count;


            uint index = 0;
            BlockingCollection<ExpandedNodeId> referenceTargets = new BlockingCollection<ExpandedNodeId>();

            var task = Task.Run(() => {

                DateTime utcNow = DateTime.UtcNow;

                while (DateTime.UtcNow - utcNow < TimeSpan.FromSeconds(3))
                {
                    var target = new ExpandedNodeId(index++, "test.namespace");
                    testNodeState.AddReference(ReferenceTypeIds.HasComponent, false, target);
                    referenceTargets.Add(target);
                }

                referenceTargets.CompleteAdding();

            });

            while (referenceTargets.TryTake(out ExpandedNodeId target, TimeSpan.FromSeconds(1)))
            {
                var removeReferenceSuccess = testNodeState.RemoveReference(ReferenceTypeIds.HasComponent, false, target);
                Assert.IsTrue(removeReferenceSuccess);
            }

            task.Wait();

            references.Clear();
            testNodeState.GetReferences(systemContext, references);

            Assert.AreEqual(originalReferenceCount, references.Count);

        }

        [Test]
        public void NodeStateNotifiersCollectionConcurrencyTest()
        {
            var serviceMessageContext = new ServiceMessageContext();
            var systemContext = new SystemContext() { NamespaceUris = serviceMessageContext.NamespaceUris };

            var testNodeState = new BaseObjectState(null);
            
            testNodeState.Create(
                new SystemContext() { NamespaceUris = serviceMessageContext.NamespaceUris },
                new NodeId("TestNode", (ushort)7),
                new QualifiedName("TestNode", (ushort)7),
                new LocalizedText("TestNode"),
                true);

            List<NodeState.Notifier> notifiers = new List<NodeState.Notifier>();

            testNodeState.GetNotifiers(systemContext, notifiers);

            int originalNotifierCount = notifiers.Count;


            uint index = 0;
            BlockingCollection<NodeState> notifierTargets = new BlockingCollection<NodeState>();

            var task = Task.Run(() => {

                DateTime utcNow = DateTime.UtcNow;

                while (DateTime.UtcNow - utcNow < TimeSpan.FromSeconds(3))
                {
                    var targetState = new AnalogUnitRangeState(null);

                    testNodeState.Create(
                        new SystemContext() { NamespaceUris = serviceMessageContext.NamespaceUris },
                        new NodeId("TestNode", (ushort)7),
                        new QualifiedName("TestNode", (ushort)7),
                        new LocalizedText("TestNode"),
                        true);

                    var target = new ExpandedNodeId(index++, "test.namespace");
                    testNodeState.AddNotifier(systemContext, ReferenceTypeIds.HasEventSource, false, targetState);
                    notifierTargets.Add(targetState);
                }

                notifierTargets.CompleteAdding();

            });

            while (notifierTargets.TryTake(out NodeState target, TimeSpan.FromSeconds(1)))
            {
                testNodeState.RemoveNotifier(systemContext, target, false);
            }

            task.Wait();

            notifiers.Clear();
            testNodeState.GetNotifiers(systemContext, notifiers);

            Assert.AreEqual(originalNotifierCount, notifiers.Count);

        }

        [Test]
        public void NodeStateChildrenCollectionConcurrencyTest()
        {
            var serviceMessageContext = new ServiceMessageContext();
            var systemContext = new SystemContext() { NamespaceUris = serviceMessageContext.NamespaceUris };

            var testNodeState = new BaseObjectState(null);

            testNodeState.Create(
                new SystemContext() { NamespaceUris = serviceMessageContext.NamespaceUris },
                new NodeId("TestNode", (ushort)7),
                new QualifiedName("TestNode", (ushort)7),
                new LocalizedText("TestNode"),
                true);

            List<BaseInstanceState> children = new List<BaseInstanceState>();

            testNodeState.GetChildren(systemContext, children);

            int originalNotifierCount = children.Count;


            uint index = 0;
            BlockingCollection<BaseInstanceState> childrenCollection = new BlockingCollection<BaseInstanceState>();

            var task = Task.Run(() => {

                DateTime utcNow = DateTime.UtcNow;

                while (DateTime.UtcNow - utcNow < TimeSpan.FromSeconds(3))
                {
                    var targetState = new AnalogUnitRangeState(null);

                    testNodeState.Create(
                        new SystemContext() { NamespaceUris = serviceMessageContext.NamespaceUris },
                        new NodeId("TestNode", (ushort)7),
                        new QualifiedName("TestNode", (ushort)7),
                        new LocalizedText("TestNode"),
                        true);

                    var target = new ExpandedNodeId(index++, "test.namespace");
                    testNodeState.AddChild(targetState);
                    childrenCollection.Add(targetState);
                }

                childrenCollection.CompleteAdding();

            });

            while (childrenCollection.TryTake(out BaseInstanceState child, TimeSpan.FromSeconds(1)))
            {
                testNodeState.RemoveChild(child);
            }

            task.Wait();

            children.Clear();
            testNodeState.GetChildren(systemContext, children);

            Assert.AreEqual(originalNotifierCount, children.Count);

        }
    }
}
