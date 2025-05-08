using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

namespace DistractorClouds.DistractorTask
{
    public class DistractorGroupTrialEnumerator : IEnumerator<GroupData>
    {

        public bool ReachedEndOfSpline { get; private set; }
        public bool MovingForwardOnSpline => _movingForward;
        private int _currentGroup;
        private readonly int _groupRange;
        private bool _movingForward;
        private int _groupCount;
        private int _currentTrial;
        private readonly int _trialCount;

        public DistractorGroupTrialEnumerator(int trialCount, int groupCount, int groupRange = -1)
        {
            _trialCount = trialCount;
            _groupCount = groupCount;
            _groupRange = groupRange;
            _currentGroup = -1;
            _movingForward = true;
            _currentTrial = -1;
            ReachedEndOfSpline = false;
        }
        
        public void Dispose()
        {
            // TODO release managed resources here
        }

        public bool MoveNext()
        {
            _currentTrial++;
            _currentGroup = _movingForward ? _currentGroup + 1 : _currentGroup - 1;
            ReachedEndOfSpline = false;

            if (_currentGroup >= _groupCount || _currentGroup < 0)
            {
                _currentGroup = _movingForward ? _groupCount - 2 : 1;
                _movingForward = !_movingForward;
                ReachedEndOfSpline = true;
            }
            
            return _currentTrial < _trialCount;
        }

        public void Reset()
        {
            _currentGroup = -1;
            _movingForward = true;
            _currentTrial = -1;
            ReachedEndOfSpline = false;
        }

        public GroupData Current => new GroupData
        {
            CurrentGroup = _currentGroup,
            GroupRange = CalculateGroupRange()
        };

        private int2 CalculateGroupRange()
        {
            if (_movingForward)
            {
                return new int2(_currentGroup, _currentGroup + _groupRange);
            } 
            return new int2(_currentGroup - _groupRange, _currentGroup);
        }

        object IEnumerator.Current => Current;
    }

    public struct GroupData
    {
        public int CurrentGroup;
        public int2 GroupRange;
    }
}