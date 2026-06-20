using UnityEngine;

public static class ParkingGarageRealMapBuilder
{
    public static ParkingGarageRealIndoorBuilder.BuildResult Build()
    {
        var indoor = ParkingGarageRealIndoorBuilder.Build();
        MapFloor.SetWalkY(indoor.WalkSurfaceY);
        MapFloor.Calibrate(new Vector3(indoor.Bounds.center.x, 0f, indoor.Bounds.min.z + 3f));
        ParkingGarageDarkAtmosphere.Apply(
            indoor.Bounds,
            ParkingGarageRealIndoorBuilder.RoomHeight,
            ParkingGarageRealIndoorBuilder.FloorCount);
        MaterialURPFixer.FixHierarchy(indoor.Root);

        return indoor;
    }
}
