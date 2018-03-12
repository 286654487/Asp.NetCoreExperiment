﻿using Akka.Actor;
using Akka.Persistence;
using System;
using System.Collections.Immutable;

namespace Demo06
{
    class Program
    {
        static void Main(string[] args)
        {
            var system = ActorSystem.Create("PersistAsync");
            var persistentActor = system.ActorOf<PersistentActor>();     


            persistentActor.Tell(new Cmd("cmd data 1"));
            persistentActor.Tell(new Cmd("cmd data 2"));
            persistentActor.Tell(new Cmd("cmd data 3"));
            persistentActor.Tell(new Cmd("cmd data 4"));
            persistentActor.Tell(new Cmd("cmd data 5"));
            persistentActor.Tell("snap");

            persistentActor.Tell("print");
            Console.ReadLine();
        }
    }

    public class Cmd
    {
        public Cmd(string data)
        {
            Data = data;
        }

        public string Data { get; }
    }

    public class Evt
    {
        public Evt(string data)
        {
            Data = data;
        }

        public string Data { get; }
    }

    public class ExampleState
    {
        private readonly ImmutableList<string> _events;

        public ExampleState(ImmutableList<string> events)
        {
            _events = events;
        }

        public ExampleState() : this(ImmutableList.Create<string>())
        {
        }

        public ExampleState Updated(Evt evt)
        {
            return new ExampleState(_events.Add(evt.Data));
        }

        public int Size => _events.Count;

        public override string ToString()
        {
            return string.Join(", ", _events.Reverse());
        }
    }

    public class PersistentActor : UntypedPersistentActor
    {
        private ExampleState _state = new ExampleState();

        private void UpdateState(Evt evt)
        {
            _state = _state.Updated(evt);
        }

        private int NumEvents => _state.Size;

        protected override void OnRecover(object message)
        {
            switch (message)
            {
                case Evt evt:
                    UpdateState(evt);
                    break;
                case SnapshotOffer snapshot when snapshot.Snapshot is ExampleState:
                    _state = (ExampleState)snapshot.Snapshot;
                    break;
                case RecoveryCompleted recoveryCompleted:
                   
                    break;
            }
        }

        protected override void OnCommand(object message)
        {
            switch (message)
            {
                case Cmd cmd:
                    Persist(new Evt($"{cmd.Data}-{NumEvents}"), UpdateState);
                    Persist(new Evt($"{cmd.Data}-{NumEvents + 1}"), evt =>
                    {
                        UpdateState(evt);
                        Context.System.EventStream.Publish(evt);
                    });
                    break;
                case "snap":
                    SaveSnapshot(_state);
                    break;
                case "print":
                    Console.WriteLine(_state);
                    break;
            }
        }

        public override string PersistenceId { get; } = "sample-id-1";
    }
}