using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movimiento : MonoBehaviour
{
    public float velocidad;
    public float rotacion;

    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            rb.velocity = transform.forward * velocidad;
        }
        if (Input.GetKey(KeyCode.S))
        {
            rb.velocity = transform.forward * -velocidad;
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.Rotate(0, -rotacion, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.Rotate(0, rotacion, 0);
        }
        if (Input.GetKey(KeyCode.Space))
        {
            rb.AddForce(Vector3.up * 10);
        }

    }
}
