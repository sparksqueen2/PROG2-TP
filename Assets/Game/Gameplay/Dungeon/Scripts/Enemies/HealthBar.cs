using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    private const int SortingOrder = 200;

    Transform _mainCamera;
    Vector3 _originalScale;
    [SerializeField] GameObject _CurrentHealthBar;
    [SerializeField] EnemyHealth _healtheable;
    HealthBarColor _healthBarColor;
    Renderer _renderer;
    

    private enum HealthBarColor { Green, Yellow, Red }

    private void Awake()
    {
        _originalScale = _CurrentHealthBar.transform.localScale;
        _renderer = GetComponent<Renderer>();

        if (_renderer != null)
            _renderer.sortingOrder = SortingOrder;

        var fillRenderer = _CurrentHealthBar.GetComponent<SpriteRenderer>();
        if (fillRenderer != null)
            fillRenderer.sortingOrder = SortingOrder + 1;
    }

    void Start()
    {
        CacheMainCamera();
        _healthBarColor = HealthBarColor.Green;
        _CurrentHealthBar.GetComponent<SpriteRenderer>().color = Color.green;
        UpdateHealthBar();
    }

    void LateUpdate()
    {
        if (!TryCacheMainCamera())
            return;

        var direction = transform.position - _mainCamera.position;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    private void CacheMainCamera()
    {
        if (_mainCamera != null)
            return;

        var camera = Camera.main;
        if (camera != null)
            _mainCamera = camera.transform;
    }

    private bool TryCacheMainCamera()
    {
        if (_mainCamera != null)
            return true;

        CacheMainCamera();
        return _mainCamera != null;
    }

    public void UpdateHealthBar()
    {
        float value = ((float)_healtheable.GetCurrentHealth()) / _healtheable.GetMaxHealth();
        _CurrentHealthBar.transform.localScale = new Vector3(_originalScale.x * value, _originalScale.y, _originalScale.z);
        if (value >= 0.80f && _healthBarColor != HealthBarColor.Green)
        {
            _healthBarColor = HealthBarColor.Green;
            _CurrentHealthBar.GetComponent<SpriteRenderer>().color = Color.green;
        }
        else if (value >= 0.25f && value < 0.8f && _healthBarColor != HealthBarColor.Yellow)
        {
            _healthBarColor = HealthBarColor.Yellow;
            _CurrentHealthBar.GetComponent<SpriteRenderer>().color = Color.yellow;
        }

        else if (value < 0.25 && _healthBarColor != HealthBarColor.Red)
        {
            _healthBarColor = HealthBarColor.Red;
            _CurrentHealthBar.GetComponent<SpriteRenderer>().color = Color.red;
        } 
    }

    public void Disable()
    {
        gameObject.SetActive(false);
    }

    public void Enable()
    {
        if (_healtheable == null || _healtheable.IsDead())
            return;

        gameObject.SetActive(true);
        UpdateHealthBar();
    }
}
