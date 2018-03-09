
// Run after the player until caught or lost
Chase() {
    Chasing = true;
    var Timer = 0.0f;
    _lastPos = Player.transform.position;
    _lastPosTracked = Player.transform.position + player.transform.forward * 5.0f;
    bool BlockedByObstacle = false;
    bool GoingToLastPosition = false;

    while(GuardUtil.state == GuardUtil.State.Chase) {
        Timer += Time.deltaTime;

        if(CanSeePlayer || CanHearPlayer) {
            GoingToLastPosition = false;
            _lastPos = Player.transform.position;
            _lastPosTracked = Player.transform.position + player.transform.forward * 5.0f;

            if(NotBlockedByObstacle) {
                // RunStraightToPlayer
                _gridAgent.StopAllCoroutines();
                _gridAgent.StraightToDestination(_lastPos);

            } else if(BlockByObstacle) {
                // Try to run around obstacle

                if(!BlockByObstacle) {
                    BlockByObstacle = true;
                    _gridAgent.SetDestination(_lastPos);
                }

                while(BlockByObstacle) {
                    if(_gridAgent.HasPathFinished || Vector3.Distance(transform.position, _lastPos) <= 1.0f) {
                        BlockByObstacle = false;
                        break;
                    } else if (NotBlockedByObstacle) {
                        BlockByObstacle = false;
                        break;
                    }
                    yield return null;
                }
            }
        } else if (CannotHearPlayer || CannotSeePlayer){
            // Going near Last seen position

            var Direction = (_lastPosTracked - transform.position).normalized;
            var Distance = Vector3.Distance(_lastPosTracked, transform.position);

            if(!Physics.Raycast(transform.position, Direction, Distance, _fov.ObstacleMask)) {
                
                if(!GoingToLastPosition) {
                    GoingToLastPosition = true;
                    _gridAgent.SetDestination(_lastPosTracked);
                }

                while(GoingToLastPosition) {
                    if(_gridAgent.HasPathFinished || Vector3.Distance(transform.position, _lastPosTracked) <= 1.0f) {
                        GoingToLastPosition = false;
                    }
                    yield return null;
                }

            } else {
                if(!GoingToLastPosition) {
                    GoingToLastPosition = true;
                    _gridAgent.SetDestination(_lastPos);
                }

                while(GoingToLastPosition) {
                    if(_gridAgent.HasPathFinished || Vector3.Distance(transform.position, _lastPos) <= 1.0f) {
                        GoingToLastPosition = false;
                    }
                    yield return null;
                }
            }
        
        }


        if(Timer >= ChaseTime) {
            print("Chase time up");
            _alertSpot = _lastPos;
            _gridAgent.StopMoving();
            GuardUtil.state = GuardUtil.State.Investigate;
            break;
        }
        yield return null;
    }

    Chasing = false;
}