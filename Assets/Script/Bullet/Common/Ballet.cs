using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 12f;
    public float damage = 1f;
    public Vector2 dir = Vector2.right;
    public float life = 3f;
    float t;

    void OnEnable() { t = 0f; }
    void Update()
    {
        transform.Translate(dir * speed * Time.deltaTime, Space.World);
        t += Time.deltaTime; if (t > life) gameObject.SetActive(false);
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (gameObject.layer == LayerMask.NameToLayer("PlayerBullet") && other.CompareTag("Enemy"))
        {
            Debug.Log("Hit Enemy");
            other.GetComponent<Health>()?.Take(damage);
            gameObject.SetActive(false);
        }
        else if (gameObject.layer == LayerMask.NameToLayer("EnemyBullet") && other.CompareTag("Player"))
        {
            other.GetComponent<Health>()?.Take(damage);
            gameObject.SetActive(false);
        }
    }
}
