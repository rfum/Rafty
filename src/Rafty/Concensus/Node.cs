using System;
using System.Collections.Generic;
using Rafty.FiniteStateMachine;
using Rafty.Log;

namespace Rafty.Concensus
{
    public class Node : INode
    {
        private readonly IFiniteStateMachine _fsm;
        private readonly ILog _log;
        private readonly Func<CurrentState, List<IPeer>> _getPeers;
        private readonly IRandomDelay _random;
        private readonly Settings _settings;

        public Node(IFiniteStateMachine fsm, ILog log, Func<CurrentState, List<IPeer>> getPeers, IRandomDelay random, Settings settings)
        {
            _fsm = fsm;
            _log = log;
            _getPeers = getPeers;
            _random = random;
            _settings = settings;
        }

        public IState State { get; private set; }

        public void Start()
        {
            if(State?.CurrentState == null)
            {
                BecomeFollower(new CurrentState(Guid.NewGuid(), 0, default(Guid), 0, 0));
            }
            else
            {
                BecomeFollower(State.CurrentState);
            }
        }

        public void BecomeCandidate(CurrentState state)
        {
            State.Stop();
            var candidate = new Candidate(state, _fsm, _getPeers(state), _log, _random, this, _settings);
            State = candidate;
            candidate.BeginElection();
        }

        public void BecomeLeader(CurrentState state)
        {
            State.Stop();
            State = new Leader(state, _fsm, _getPeers(state), _log, this, _settings);
        }

        public void BecomeFollower(CurrentState state)
        {
            State?.Stop();
            State = new Follower(state, _fsm, _log, _random, this, _settings);
        }

        public AppendEntriesResponse Handle(AppendEntries appendEntries)
        {
            return State.Handle(appendEntries);
        }

        public RequestVoteResponse Handle(RequestVote requestVote)
        {
            return State.Handle(requestVote);
        }

        public Response<T> Accept<T>(T command)
        {
            return State.Accept(command);
        }
        public void Stop()
        {
            State.Stop();
        }
    }
}