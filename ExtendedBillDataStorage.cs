﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HugsLib.Utils;
using ImprovedWorkbenches;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class ExtendedBillDataStorage : UtilityWorldObject, IExposable
    {
        private Dictionary<int, ExtendedBillData> _store =
            new Dictionary<int, ExtendedBillData>();

        private List<int> _billIDsWorkingList;

        private List<ExtendedBillData> _extendedBillDataWorkingList;

        private static readonly FieldInfo LoadIdGetter = typeof(Bill).GetField("loadID",
            BindingFlags.NonPublic | BindingFlags.Instance);

        public override void ExposeData()
        {
            base.ExposeData();
			Scribe_Collections.Look(
                ref _store, "store", 
                LookMode.Value, LookMode.Deep, 
                ref _billIDsWorkingList, ref _extendedBillDataWorkingList);
        }

        // Return the associate extended data for a given bill, creating a new association
        // if required.
        public ExtendedBillData GetExtendedDataFor(Bill_Production bill)
        {

            var loadId = (int) LoadIdGetter.GetValue(bill);
            if (_store.TryGetValue(loadId, out ExtendedBillData data))
            {
                return data;
            }

            ExtendedBillData newExtendedData;
            if (bill is IBillWithThingFilter)
            {
                Main.Instance.Logger.Warning(
                    $"Found old Bill ({bill.GetUniqueLoadID()}), migrating to new format");

                newExtendedData = new ExtendedBillData(bill);
            }
            else
            {
                Main.Instance.Logger.Message(
                    $"Creating new data for {bill.GetUniqueLoadID()}");
                newExtendedData = new ExtendedBillData();
                if (CanOutputBeFiltered(bill))
                    newExtendedData.SetDefaultFilter(bill);
            }

            _store[loadId] = newExtendedData;
            return newExtendedData;
        }

        // Delete extended data when bill is deleted
        public void DeleteExtendedDataFor(Bill_Production bill)
        {
            var loadId = (int) LoadIdGetter.GetValue(bill);
            _store.Remove(loadId);
        }

        // Figure out if output of bill produces a "thing" with quality or hit-points
        internal static bool CanOutputBeFiltered(Bill_Production bill)
        {
            return CanOutputBeFiltered(bill.recipe);
        }

        // Figure out if output of recipe produces a "thing" with quality or hit-points
        private static bool CanOutputBeFiltered(RecipeDef recipe)
        {
            if (recipe.products == null || recipe.products.Count == 0)
                return false;

            var thingDef = recipe.products.First().thingDef;
            if (thingDef.BaseMarketValue <= 0)
                return false;

            return !thingDef.CountAsResource;
        }

    }
}