using System;
using System.Collections.Generic;
using UnityEngine;

// Kho vật phẩm đã nhặt trong lần chơi này (không lưu qua lần chơi sau).
public static class Inventory
{
    private static readonly List<Item> items = new List<Item>();

    public static IReadOnlyList<Item> Items => items;
    public static event Action<Item> ItemAdded;

    public static bool Has(Item item) => item != null && items.Contains(item);

    // Trả về false nếu item rỗng hoặc đã có trong kho.
    public static bool Add(Item item)
    {
        if (item == null || items.Contains(item))
            return false;
        items.Add(item);
        ItemAdded?.Invoke(item);
        return true;
    }

    // Tắt Domain Reload khi vào Play thì static không tự xóa: dọn tay.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        items.Clear();
        ItemAdded = null;
    }
}
