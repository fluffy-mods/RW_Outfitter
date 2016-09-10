using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Outfitter
{
    public class SaveablePawn : IExposable
    {
        // Exposed members
        public Pawn Pawn;

        public bool TargetTemperaturesOverride;
        public bool AddWorkStats = true;
        public bool AddIndividualStats = true;

        public FloatRange Temperatureweight;

        public FloatRange TargetTemperatures;
        public FloatRange RealComfyTemperatures;

        public bool forceStatUpdate = false;

        public enum MainJob
        {
            Anything,
            Soldier00Close_Combat,
            Soldier00Ranged_Combat,
            Artist,
            Constructor,
            Cook,
            Crafter,
            Doctor,
            Grower,
            Handler,
            Hauler,
            Hunter,
            Miner,
            Researcher,
            Smith,
            Tailor,
            Warden
        }

        public MainJob mainJob;

        public List<Saveable_Pawn_StatDef> Stats = new List<Saveable_Pawn_StatDef>();
        public bool SetRealComfyTemperatures;

        //  public SaveablePawn(Pawn pawn)
        //    {
        //        Pawn = pawn;
        //        Stats = new List<Saveable_Pawn_StatDef>();
        //        _lastStatUpdate = -5000;
        //        _lastTempUpdate = -5000;
        //    }


        public void ExposeData()
        {
            Scribe_References.LookReference(ref Pawn, "Pawn");
            Scribe_Values.LookValue(ref TargetTemperaturesOverride, "targetTemperaturesOverride");
            Scribe_Values.LookValue(ref TargetTemperatures, "TargetTemperatures");
            Scribe_Values.LookValue(ref SetRealComfyTemperatures, "SetRealComfyTemperatures");
            Scribe_Values.LookValue(ref RealComfyTemperatures, "RealComfyTemperatures");
            Scribe_Values.LookValue(ref Temperatureweight, "Temperatureweight");
            Scribe_Collections.LookList(ref Stats, "Stats", LookMode.Deep);
            Scribe_Values.LookValue(ref AddWorkStats, "AddWorkStats", true);
            Scribe_Values.LookValue(ref AddIndividualStats, "AddIndividualStats", true);
            Scribe_Values.LookValue(ref mainJob, "mainJob");


        }
    }
}