﻿using System.Collections.Generic;
using UnityEngine;

namespace GMTK2020.Data
{

    public abstract class SimulationStep
    {
        public abstract bool FinalStep { get; }
    }

    public class MatchStep : SimulationStep
    {
        public override bool FinalStep => false;

        public HashSet<Tile> MatchedTiles { get; }
        public List<MovedTile> MovedTiles { get; }

        public HashSet<Vector2Int> LeftEndsOfHorizontalMatches { get; }
        public HashSet<Vector2Int> BottomEndsOfVerticalMatches { get; }

        public MatchStep(HashSet<Tile> matchedTiles, List<MovedTile> movingTiles, HashSet<Vector2Int> leftEndsOfHorizontalMatches, HashSet<Vector2Int> bottomEndsOfVerticalMatches)
        {
            MatchedTiles = matchedTiles;
            MovedTiles = movingTiles;
            LeftEndsOfHorizontalMatches = leftEndsOfHorizontalMatches;
            BottomEndsOfVerticalMatches = bottomEndsOfVerticalMatches;
        }
    }

    public class CleanUpStep : SimulationStep
    {
        public override bool FinalStep => true;

        public List<MovedTile> NewTiles { get; }
        public HashSet<Tile> InertTiles { get; }

        public CleanUpStep(List<MovedTile> newTiles, HashSet<Tile> inertTiles)
        {
            NewTiles = newTiles;
            InertTiles = inertTiles;
        }
    }

    public class RemovalStep : SimulationStep
    {
        public override bool FinalStep => true;

        public HashSet<Tile> RemovedTiles { get; }
        public List<MovedTile> MovedTiles { get; }
        public List<MovedTile> NewTiles { get; }

        public RemovalStep(HashSet<Tile> removedTiles, List<MovedTile> movedTiles, List<MovedTile> newTiles)
        {
            RemovedTiles = removedTiles;
            MovedTiles = movedTiles;
            NewTiles = newTiles;
        }
    }

    public class PermutationStep : SimulationStep
    {
        public override bool FinalStep => true;

        public List<MovedTile> MovedTiles { get; }

        public PermutationStep(List<MovedTile> movedTiles)
        {
            MovedTiles = movedTiles;
        }
    }

    public class RotationStep : SimulationStep
    {
        public override bool FinalStep => true;

        public Vector2 Pivot { get; }
        public RotationSense RotationSense { get; }
        public List<MovedTile> MovedTiles { get; }

        public RotationStep(Vector2 pivot, RotationSense rotationSense, List<MovedTile> movedTiles)
        {
            Pivot = pivot;
            RotationSense = rotationSense;
            MovedTiles = movedTiles;
        }
    }
}