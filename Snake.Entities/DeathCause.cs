namespace Snake.Entities;

public enum DeathCause
{
    SelfCollision,
    BoundaryCollision,
    WallCollision,
    ClosedDoorCollision,
    Bomb,
    FatalFire
}
