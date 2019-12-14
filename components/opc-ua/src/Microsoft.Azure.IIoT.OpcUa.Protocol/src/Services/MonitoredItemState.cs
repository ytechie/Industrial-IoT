// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Monitored item diffing engine
    /// </summary>
    public class MonitoredItemState {

        /// <summary>
        /// Assigned monitored item id on server
        /// </summary>
        public uint? ServerId => Reported?.Status.Id;

        /// <summary>
        /// Desired Monitored item state
        /// </summary>
        public MonitoredItemModel Desired { get; }

        /// <summary>
        /// Reported monitored item state
        /// </summary>
        public MonitoredItemModel Reported { get; private set; }

        /// <summary>
        /// Create wrapper
        /// </summary>
        /// <param name="template"></param>
        /// <param name="logger"></param>
        public MonitoredItemState(MonitoredItemModel template, ILogger logger) {
            _logger = logger?.ForContext<MonitoredItemState>() ??
                throw new ArgumentNullException(nameof(logger));
            Desired = template.Clone() ??
                throw new ArgumentNullException(nameof(template));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is MonitoredItemState item)) {
                return false;
            }
            if (Desired.Id != item.Reported.Id) {
                return false;
            }
            if (!Desired.RelativePath.SequenceEqualsSafe(item.Reported.RelativePath)) {
                return false;
            }
            if (Desired.StartNodeId != item.Reported.StartNodeId) {
                return false;
            }
            if (Desired.IndexRange != item.Reported.IndexRange) {
                return false;
            }
            if (Desired.AttributeId != item.Reported.AttributeId) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = 1301977042;
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(Desired.Id);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string[]>.Default.GetHashCode(Desired.RelativePath);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(Desired.StartNodeId);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(Desired.IndexRange);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<NodeAttribute?>.Default.GetHashCode(Desired.AttributeId);
            return hashCode;
        }

        /// <inheritdoc/>
        public override string ToString() {
            return $"Item {Desired.Id ?? "<unknown>"}{ServerId}: '{Desired.StartNodeId}'" +
                $" - {(Reported?.Status?.Created == true ? "" : "not ")}created";
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
        internal MonitoringMode? GetMonitoringModeChange() {
            var change = _modeChange;
            _modeChange = null;
            return Reported.MonitoringMode == change ? null : change;
        }

        /// <summary>
        /// Synchronize monitored items and triggering configuration in subscription
        /// </summary>
        /// <param name="monitoredItems"></param>
        /// <param name="currentItems"></param>
        /// <param name="deletes"></param>
        /// <param name="add"></param>
        /// <param name="update"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static bool Diff(IEnumerable<MonitoredItemModel> monitoredItems,
            IEnumerable<MonitoredItemState> currentItems,
            out HashSet<MonitoredItemState> deletes,
            out HashSet<MonitoredItemState> add,
            out HashSet<MonitoredItemState> update,
            ILogger logger) {

            update = new HashSet<MonitoredItemState>();
            add = new HashSet<MonitoredItemState>();
            if (monitoredItems == null) {
                deletes = new HashSet<MonitoredItemState>();
                return false;
            }

            // Synchronize the desired items with the state of the raw subscription
            var desiredState = monitoredItems
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
            //
            //      rawSubscription.RemoveItem(detached);
            //  }

            var nowMonitored = new List<MonitoredItemState>();

            // Add new monitored items not in current state
            foreach (var toAdd in desiredState.Except(currentState)) {
                logger.Debug("Adding new monitored item '{item}'...", toAdd);
                add.Add(toAdd);
                nowMonitored.Add(toAdd);
                applyChanges = true;
            }

            // Update monitored items that have changed
            var desiredUpdates = desiredState.Intersect(currentState)
                .ToDictionary(k => k, v => v);
            foreach (var toUpdate in currentState.Intersect(desiredState)) {
                if (!toUpdate.Equals(desiredUpdates[toUpdate])) {
                    logger.Debug("Updating monitored item '{item}'...", toUpdate);
                    update.Add(toUpdate);
                    applyChanges = true;
                }
                nowMonitored.Add(toUpdate);
            }
        }

        /// <summary>
        /// Get trigger links from all items
        /// </summary>
        /// <param name="nowMonitored"></param>
        /// <param name="added"></param>
        /// <param name="removed"></param>
        /// <param name="modeChanges"></param>
        /// <returns></returns>
        public static void GetTriggerLinks(IEnumerable<MonitoredItemState> nowMonitored,
            out HashSet<uint> added, out HashSet<uint> removed,
            out Dictionary<uint, MonitoringMode> modeChanges) {

            added = new HashSet<uint>();
            removed = new HashSet<uint>();
            modeChanges = new Dictionary<uint, MonitoringMode>();

            var map = nowMonitored.ToDictionary(
                k => k.Reported.Id ?? k.Reported.StartNodeId, v => v);
            foreach (var item in nowMonitored.ToList()) {
                if (item.Desired.TriggerId != null &&
                    map.TryGetValue(item.Desired.TriggerId, out var trigger)) {
                    trigger.AddTriggerLink(item.ServerId);
                }
            }

            // Set up any new trigger configuration if needed
            foreach (var item in nowMonitored.ToList()) {
                item.GetTriggeringLinks(ref added, ref removed);
            }

            // Change monitoring mode of all items if necessary
            foreach (var change in nowMonitored.GroupBy(i => i.GetMonitoringModeChange())) {
                if (change.Key == null) {
                    continue;
                }
                await rawSubscription.Session.SetMonitoringModeAsync(null,
                    rawSubscription.Id, change.Key.Value,
                    new UInt32Collection(change.Select(i => i.ServerId ?? 0)));
            }

            _currentlyMonitored = nowMonitored;

            // Set timer to check connection periodically
            if (_currentlyMonitored.Count > 0) {
                _timer.Change(TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
            }
        }


        private HashSet<uint> _newTriggers = new HashSet<uint>();
        private HashSet<uint> _triggers = new HashSet<uint>();
        private Publisher.Models.MonitoringMode? _modeChange;
        private readonly ILogger _logger;
    }
}