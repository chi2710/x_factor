using UnityEngine;

// Hướng mặt của nhân vật 2D, cùng quy ước với Player: quay mặt trái = rotation Y 180.
// Cũng tính cả trường hợp lật bằng scale.x âm.
public static class Facing2D
{
    public static float Get(Transform t)
    {
        return Mathf.Sign(t.right.x) * Mathf.Sign(t.lossyScale.x);
    }

    public static void Set(Transform t, float dir)
    {
        if (dir != 0f && Mathf.Sign(dir) != Get(t))
            t.rotation = dir < 0f ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
    }

    // Đi ngang theo dir (-1/0/1). Có Rigidbody2D thì đặt vận tốc ngang, không thì dời transform.
    public static void MoveX(Transform t, Rigidbody2D rb, float dir, float speed)
    {
        if (rb != null)
            rb.linearVelocityX = dir * speed;
        else
            t.position += Vector3.right * (dir * speed * Time.deltaTime);
    }
}
