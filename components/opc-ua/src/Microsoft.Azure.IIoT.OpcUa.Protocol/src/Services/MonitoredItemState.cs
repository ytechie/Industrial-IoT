// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua.Client;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Monitored item diffing engine
    /// </summary>
    public class MonitoredItemState {

        /// <summary>
        /// Client Id
        /// </summary>
        public string ClientId => MonitoredItem.Id;

        /// <summary>
        /// Server id
        /// </summary>
        public uint? ServerId => Status.ServerId;

        /// <summary>
        /// Status
        /// </summary>
        public MonitoredItemStatusModel Status { get; private set; }

        /// <summary>
        /// Monitored item model
        /// </summary>
        public MonitoredItemModel MonitoredItem { get; set; }

        /// <summary>
        /// Create wrapper
        /// </summary>
        /// <param name="monitoredItem"></param>
        /// <param name="logger"></param>
        public MonitoredItemState(MonitoredItemModel monitoredItem, ILogger logger) {
            _logger = logger?.ForContext<MonitoredItemState>() ??
                throw new ArgumentNullException(nameof(logger));
            MonitoredItem = monitoredItem.Clone() ??
                throw new ArgumentNullException(nameof(monitoredItem));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is MonitoredItemState item)) {
                return false;
            }
            if (MonitoredItem.Id != item.MonitoredItem.Id) {
                return false;
            }
            if (!MonitoredItem.RelativePath.SequenceEqualsSafe(item.MonitoredItem.RelativePath)) {
                return false;
            }
            if (MonitoredItem.StartNodeId != item.MonitoredItem.StartNodeId) {
                return false;
            }
            if (MonitoredItem.IndexRange != item.MonitoredItem.IndexRange) {
                return false;
            }
            if (MonitoredItem.AttributeId != item.MonitoredItem.AttributeId) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = 1301977042;
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(MonitoredItem.Id);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string[]>.Default.GetHashCode(MonitoredItem.RelativePath);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(MonitoredItem.StartNodeId);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(MonitoredItem.IndexRange);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<NodeAttribute?>.Default.GetHashCode(MonitoredItem.AttributeId);
            return hashCode;
        }

        /// <inheritdoc/>
        public override string ToString() {
            return $"Item {MonitoredItem.Id ?? "<unknown>"}{Status.ServerId}: " +
                $"'{MonitoredItem.StartNodeId}'" +
                $" - {(Status.ServerId != null ? "" : "not ")}created";
        }

        //  /// <summary>
        //  /// Create new
        //  /// </summary>
        //  /// <param name="session"></param>
        //  /// <returns></returns>
        //  internal void Create(Session session) {
        //      Reported = new MonitoredItem {
        //          Handle = this,
        //
        //          DisplayName = Desired.DisplayName,
        //          AttributeId = ((uint?)Desired.AttributeId) ?? Attributes.Value,
        //          IndexRange = Desired.IndexRange,
        //          RelativePath = Desired.RelativePath?
        //                      .ToRelativePath(session.MessageContext)?
        //                      .Format(session.NodeCache.TypeTree),
        //          MonitoringMode = Desired.MonitoringMode.ToStackType() ??
        //              Opc.Ua.MonitoringMode.Reporting,
        //          StartNodeId = Desired.StartNodeId.ToNodeId(session.MessageContext),
        //          QueueSize = Desired.QueueSize ?? 0,
        //          SamplingInterval =
        //              (int?)Desired.SamplingInterval?.TotalMilliseconds ?? -1,
        //          DiscardOldest = !(Desired.DiscardNew ?? false),
        //          Filter =
        //              Desired.DataChangeFilter
        //                  .ToStackModel() ??
        //              Desired.EventFilter
        //                  .ToStackModel(session.MessageContext, true) ??
        //              ((MonitoringFilter)Desired.AggregateFilter
        //                  .ToStackModel(session.MessageContext))
        //      };
        //  }

        /// <summary>
        /// Add the monitored item identifier of the triggering item.
        /// </summary>
        /// <param name="id"></param>
        internal void AddTriggerLink(uint? id) {
            if (id != null) {
                _newTriggers.Add(id.Value);
            }
        }

    //   /// <summary>
    //   /// Merge with desired state
    //   /// </summary>
    //   /// <param name="model"></param>
    //   internal bool MergeWith(MonitoredItemWrapper model) {
    //
    //       if (model == null || Reported == null) {
    //           return false;
    //       }
    //
    //       var changes = false;
    //
    //       if (((int?)Desired.SamplingInterval?.TotalMilliseconds ?? -1) !=
    //               ((int?)model.Template.SamplingInterval?.TotalMilliseconds ?? -1)) {
    //           _logger.Debug("{item}: Changing sampling interval from {old} to {new}",
    //               this, (int?)Desired.SamplingInterval?.TotalMilliseconds ?? -1,
    //               (int?)model.Template.SamplingInterval?.TotalMilliseconds ?? -1);
    //           Desired.SamplingInterval = model.Template.SamplingInterval;
    //           Reported.SamplingInterval =
    //               (int?)Desired.SamplingInterval?.TotalMilliseconds ?? -1;
    //           changes = true;
    //       }
    //
    //       if ((Desired.DiscardNew ?? false) !=
    //               (model.Template.DiscardNew ?? false)) {
    //           _logger.Debug("{item}: Changing discard new mode from {old} to {new}",
    //               this, Desired.DiscardNew ?? false, model.Template.DiscardNew ?? false);
    //           Desired.DiscardNew = model.Template.DiscardNew;
    //           Reported.DiscardOldest = !(Desired.DiscardNew ?? false);
    //           changes = true;
    //       }
    //
    //       if ((Desired.QueueSize ?? 0) != (model.Template.QueueSize ?? 0)) {
    //           _logger.Debug("{item}: Changing queue size from {old} to {new}",
    //               this, Desired.QueueSize ?? 0, model.Template.QueueSize ?? 0);
    //           Desired.QueueSize = model.Template.QueueSize;
    //           Reported.QueueSize = Desired.QueueSize ?? 0;
    //           changes = true;
    //       }
    //
    //       if ((Desired.MonitoringMode ?? Publisher.Models.MonitoringMode.Reporting) !=
    //           (model.Template.MonitoringMode ?? Publisher.Models.MonitoringMode.Reporting)) {
    //           _logger.Debug("{item}: Changing monitoring mode from {old} to {new}",
    //               this,
    //               Desired.MonitoringMode ?? Publisher.Models.MonitoringMode.Reporting,
    //               model.Template.MonitoringMode ?? Publisher.Models.MonitoringMode.Reporting);
    //           Desired.MonitoringMode = model.Template.MonitoringMode;
    //           _modeChange = Desired.MonitoringMode ??
    //               Publisher.Models.MonitoringMode.Reporting;
    //       }
    //
    //       // TODO
    //       // monitoredItem.Filter = monitoredItemInfo.Filter?.ToStackType();
    //       return changes;
    //   }

        /// <summary>
        /// Get triggering configuration changes for this item
        /// </summary>
        /// <param name="addLinks"></param>
        /// <param name="removeLinks"></param>
        /// <returns></returns>
        internal bool GetTriggeringLinks(ref HashSet<uint> addLinks,
            ref HashSet<uint> removeLinks) {
            var remove = _triggers.Except(_newTriggers).ToList();
            var add = _newTriggers.Except(_triggers).ToList();
            _triggers = _newTriggers;
            _newTriggers = new HashSet<uint>();
            foreach (var item in add) addLinks.Add(item);
            foreach (var item in remove) removeLinks.Add(item);
            if (add.Count > 0 || remove.Count > 0) {
                _logger.Debug("{item}: Adding {add} links and removing {remove} links.",
                    this, add.Count, remove.Count);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get any changes in the monitoring mode
        /// </summary>
        /// <returns></returns>
        internal MonitoringItemMode? GetMonitoringModeChange() {
            var change = _modeChange;
            _modeChange = null;
            return MonitoredItem.MonitoringMode == change ? null : change;
        }

        /// <summary>
        /// Test full equality across all configuration settings
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool IsEqualConfiguration(object obj) {
            if (!Equals(obj)) {
                return false;
            }
            var item = obj as MonitoredItemState;
            if (MonitoredItem.DiscardNew != item.MonitoredItem.DiscardNew) {
                return false;
            }
            if (!MonitoredItem.AggregateFilter.IsSameAs(item.MonitoredItem.AggregateFilter)) {
                return false;
            }
            if (!MonitoredItem.EventFilter.IsSameAs(item.MonitoredItem.EventFilter)) {
                return false;
            }
            if (!MonitoredItem.DataChangeFilter.IsSameAs(item.MonitoredItem.DataChangeFilter)) {
                return false;
            }
            if (MonitoredItem.MonitoringMode != item.MonitoredItem.MonitoringMode) {
                return false;
            }
            if (MonitoredItem.QueueSize != item.MonitoredItem.QueueSize) {
                return false;
            }
            if (MonitoredItem.SamplingInterval != item.MonitoredItem.SamplingInterval) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Synchronize monitored items and triggering configuration in subscription
        /// </summary>
        /// <param name="desiredItems"></param>
        /// <param name="currentItems"></param>
        /// <param name="deletes"></param>
        /// <param name="add"></param>
        /// <param name="update"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static bool GetMonitoredItemChangesPhase1(
            IEnumerable<MonitoredItemModel> desiredItems,
            IEnumerable<MonitoredItemState> currentItems,
            out HashSet<MonitoredItemState> deletes,
            out HashSet<MonitoredItemState> add,
            out HashSet<MonitoredItemState> update,
            ILogger logger) {

            update = new HashSet<MonitoredItemState>();
            add = new HashSet<MonitoredItemState>();
            if (desiredItems == null) {
                deletes = new HashSet<MonitoredItemState>();
                return false;
            }

            // Synchronize the desired items with the state of the raw subscription
            var desiredState = desiredItems
                .Select(m => new MonitoredItemState(m, logger))
                .ToHashSetSafe();
            var currentState = currentItems
                .ToHashSetSafe();

            // Remove monitored items not in desired state
            deletes = currentState.Except(desiredState).ToHashSetSafe();
            var applyChanges = deletes.Count != 0;

            //  // Re-associate detached handles
            //  foreach (var detached in rawSubscription.MonitoredItems
            //      .Where(m => m.Handle == null)) {
            //
            //      // TODO: Claim monitored item
            //
            //      rawSubscription.RemoveItem(detached);
            //  }

            // Add new monitored items not in current state
            foreach (var toAdd in desiredState.Except(currentState)) {
                logger.Debug("Adding new monitored item '{item}'...", toAdd);
                add.Add(toAdd);
                applyChanges = true;
            }

            // Update monitored items that have changed
            var desiredUpdates = desiredState.Intersect(currentState)
                .ToDictionary(k => k, v => v);
            foreach (var toUpdate in currentState.Intersect(desiredState)) {
                if (!toUpdate.IsEqualConfiguration(desiredUpdates[toUpdate])) {
                    logger.Debug("Updating monitored item '{item}'...", toUpdate);
                    update.Add(toUpdate);
                    applyChanges = true;
                }
            }
            return applyChanges;
        }

        /// <summary>
        /// Get trigger links from all items
        /// </summary>
        /// <param name="currentItems"></param>
        /// <param name="added"></param>
        /// <param name="removed"></param>
        /// <param name="modeChanges"></param>
        /// <returns></returns>
        public static void GetMonitoredItemChangesPhase2(IEnumerable<MonitoredItemState> currentItems,
            out HashSet<uint> added, out HashSet<uint> removed,
            out Dictionary<MonitoringItemMode, List<uint>> modeChanges) {

            added = new HashSet<uint>();
            removed = new HashSet<uint>();
            modeChanges = new Dictionary<MonitoringItemMode, List<uint>>();

            var map = currentItems.ToDictionary(
                k => k.MonitoredItem.Id ?? k.MonitoredItem.StartNodeId, v => v);
            foreach (var item in currentItems.ToList()) {
                if (item.MonitoredItem.TriggerId != null &&
                    map.TryGetValue(item.MonitoredItem.TriggerId, out var trigger)) {
                    trigger.AddTriggerLink(item.Status.ServerId);
                }
            }

            // Set up any new trigger configuration if needed
            foreach (var item in currentItems.ToList()) {
                item.GetTriggeringLinks(ref added, ref removed);
            }

            // Change monitoring mode of all items if necessary
            foreach (var change in currentItems.GroupBy(i => i.GetMonitoringModeChange())) {
                if (change.Key == null) {
                    continue;
                }
                modeChanges.Add(change.Key.Value,
                    change.Select(i => i.Status.ServerId ?? 0).ToList());
            }
        }


        private HashSet<uint> _newTriggers = new HashSet<uint>();
        private HashSet<uint> _triggers = new HashSet<uint>();
        private MonitoringItemMode? _modeChange;
        private readonly ILogger _logger;
    }
}