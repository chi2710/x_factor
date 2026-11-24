using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Cainos.LucidEditor.Experimental
{
    internal class SimpleTreeView : TreeView<EntityId>
    {
        private TreeMenuItem[] baseElements;

        public Action<Rect, EntityId> drawItemCallback;
        public Func<EntityId, float> itemHeightCallback;
        public event Action<IList<EntityId>> onSelectionChanged;

        public SimpleTreeView(TreeViewState<EntityId> treeViewState) : base(treeViewState) { }

        public void Setup(TreeMenuItem[] baseElements)
        {
            this.baseElements = baseElements;
            Reload();
        }

        protected override TreeViewItem<EntityId> BuildRoot()
        {
            return new TreeViewItem<EntityId> { id = -1, depth = -1, displayName = "Root" };
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (drawItemCallback != null)
            {
                Rect contentRect = args.rowRect;
                contentRect.width -= GetContentIndent(args.item);
                contentRect.x += GetContentIndent(args.item);

                drawItemCallback.Invoke(contentRect, args.item.id);
            }
            else
            {
                base.RowGUI(args);
            }
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem<EntityId> item)
        {
            if (itemHeightCallback != null)
            {
                return itemHeightCallback.Invoke(item.id);
            }
            return base.GetCustomRowHeight(row, item);
        }

        protected override IList<TreeViewItem<EntityId>> BuildRows(TreeViewItem<EntityId> root)
        {
            var rows = GetRows() ?? new List<TreeViewItem<EntityId>>();
            rows.Clear();

            foreach (var baseElement in baseElements)
            {
                var baseItem = CreateTreeViewItem(baseElement);
                root.AddChild(baseItem);
                rows.Add(baseItem);
                if (baseElement.childElements.Count > 0)
                {
                    if (IsExpanded(baseItem.id))
                    {
                        AddChildrenRecursive(baseElement, baseItem, rows);
                    }
                    else
                    {
                        baseItem.children = CreateChildListForCollapsedParent();
                    }
                }
            }

            SetupDepthsFromParentsAndChildren(root);

            return rows;
        }

        protected override void SelectionChanged(IList<EntityId> selectedIds)
        {
            onSelectionChanged?.Invoke(selectedIds);
        }

        private void AddChildrenRecursive(TreeMenuItem model, TreeViewItem<EntityId> item, IList<TreeViewItem<EntityId>> rows)
        {
            foreach (var childElement in model.childElements)
            {
                var childItem = CreateTreeViewItem(childElement);
                item.AddChild(childItem);
                rows.Add(childItem);
                if (childElement.childElements.Count > 0)
                {
                    if (IsExpanded(childElement.id))
                    {
                        AddChildrenRecursive(childElement, childItem, rows);
                    }
                    else
                    {
                        childItem.children = CreateChildListForCollapsedParent();
                    }
                }
            }
        }

        private TreeViewItem<EntityId> CreateTreeViewItem(TreeMenuItem model)
        {
            return new TreeViewItem<EntityId> { id = model.id, displayName = model.name };
        }
    }
}