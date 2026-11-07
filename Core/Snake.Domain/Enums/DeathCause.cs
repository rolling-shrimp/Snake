namespace Snake.Domain
{
    public enum DeathCause
    {
        SelfCollision,
        BoundaryCollision,
        WallCollision,
        ClosedDoorCollision,
        Bomb,
        FatalFire
    }
}
