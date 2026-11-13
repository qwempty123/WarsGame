using System.Collections.Generic;
using UnityEngine;

public class UnitSelection : MonoBehaviour
{
    [Header("Настройки выделения")]
    [SerializeField] private float dragThreshold = 5f;
    [SerializeField, Min(1.5f)] private float formationSpacing = 2f;

    private List<Tank_T90> selectedTanks = new List<Tank_T90>();

    private Vector3 dragStartWorld;
    private bool isDragging = false;
    private bool isBoxSelecting = false;

    // Враг, на которого сейчас наведена мышь
    private Tank_T90 hoveredEnemy = null;

    void Update()
    {
        RemoveUnavailableUnitsFromSelection();

        // --- ЛЕВАЯ КНОПКА МЫШИ (ВЫДЕЛЕНИЕ) ---
        if (Input.GetMouseButtonDown(0))
        {
            dragStartWorld = GetMouseWorldPos();
            isDragging = true;
            isBoxSelecting = false;
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 startScreen = Camera.main.WorldToScreenPoint(dragStartWorld);
            Vector3 currentScreen = Input.mousePosition;

            if (Vector3.Distance(startScreen, currentScreen) > dragThreshold)
            {
                isBoxSelecting = true;
            }
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;

            if (isBoxSelecting)
            {
                SelectUnitsInBox(dragStartWorld, GetMouseWorldPos(),
                    Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            }
            else
            {
                HandleSingleClick();
            }
            isBoxSelecting = false;
        }

        // --- ПРАВАЯ КНОПКА МЫШИ (ПРИКАЗЫ) ---
        if (Input.GetMouseButtonDown(1))
        {
            IssueCommand(GetMouseWorldPos());
        }

        // --- ОБНОВЛЕНИЕ НАВЕДЕНИЯ НА ВРАГА (ДЛЯ ЛИНИЙ) ---
        UpdateHoveredEnemy();

        // Переключение боеприпасов для всех выбранных танков.
        HandleAmmoHotkeys();
    }

    private void RemoveUnavailableUnitsFromSelection()
    {
        for (int i = selectedTanks.Count - 1; i >= 0; i--)
        {
            Tank_T90 tank = selectedTanks[i];
            if (tank != null && !tank.IsDead) continue;

            if (tank != null) tank.SetSelected(false);
            selectedTanks.RemoveAt(i);
        }
    }

    private void HandleAmmoHotkeys()
    {
        ShellType? requestedType = null;

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            requestedType = ShellType.AP;
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            requestedType = ShellType.HEAT;
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            requestedType = ShellType.HE;

        if (!requestedType.HasValue) return;

        foreach (Tank_T90 tank in selectedTanks)
        {
            if (tank == null) continue;

            TankCombat combat = tank.GetComponent<TankCombat>();
            if (combat != null) combat.SelectShell(requestedType.Value);
        }
    }

    private void UpdateHoveredEnemy()
    {
        hoveredEnemy = null;
        if (selectedTanks.Count == 0) return;

        Vector3 mouseWorld = GetMouseWorldPos();
        Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorld);

        if (hitCollider != null)
        {
            Tank_T90 tank = hitCollider.GetComponent<Tank_T90>();
            if (tank == null) tank = hitCollider.GetComponentInParent<Tank_T90>();

            // Если навели на врага (ID не равен 0)
            if (tank != null && tank.teamId != 0 && !tank.IsDead)
            {
                hoveredEnemy = tank;
            }
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        return mouseWorld;
    }

    // Обычный клик левой кнопкой
    private void HandleSingleClick()
    {
        Vector3 mouseWorld = GetMouseWorldPos();
        Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorld);
        Tank_T90 clickedTank = null;

        if (hitCollider != null)
        {
            clickedTank = hitCollider.GetComponent<Tank_T90>();
            if (clickedTank == null) clickedTank = hitCollider.GetComponentInParent<Tank_T90>();
        }

        if (clickedTank != null && clickedTank.teamId == 0 && !clickedTank.IsDead)
        {
            bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (additive && selectedTanks.Contains(clickedTank))
            {
                selectedTanks.Remove(clickedTank);
                clickedTank.SetSelected(false);
                return;
            }

            if (!additive) ClearSelection();

            if (!selectedTanks.Contains(clickedTank))
            {
                selectedTanks.Add(clickedTank);
                clickedTank.SetSelected(true);
            }
            return;
        }

        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (!shiftHeld) ClearSelection();
    }

    // Выделение рамкой
    private void SelectUnitsInBox(Vector3 startPos, Vector3 endPos, bool additive)
    {
        if (!additive) ClearSelection();

        Collider2D[] colliders = Physics2D.OverlapAreaAll(startPos, endPos);

        foreach (var col in colliders)
        {
            Tank_T90 tank = col.GetComponent<Tank_T90>();
            if (tank == null) tank = col.GetComponentInParent<Tank_T90>();

            if (tank != null && tank.teamId == 0 && !tank.IsDead)
            {
                if (!selectedTanks.Contains(tank))
                {
                    selectedTanks.Add(tank);
                    tank.SetSelected(true);
                }
            }
        }
    }

    private void ClearSelection()
    {
        foreach (var tank in selectedTanks)
        {
            if (tank != null) tank.SetSelected(false);
        }
        selectedTanks.Clear();
    }

    // Приказы правой кнопкой мыши
    private void IssueCommand(Vector3 mouseWorld)
    {
        RemoveUnavailableUnitsFromSelection();
        if (selectedTanks.Count == 0) return;

        Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorld);
        Tank_T90 clickedTank = null;

        if (hitCollider != null)
        {
            clickedTank = hitCollider.GetComponent<Tank_T90>();
            if (clickedTank == null) clickedTank = hitCollider.GetComponentInParent<Tank_T90>();
        }

        // Правый клик по живому противнику — приказ атаки.
        if (clickedTank != null && clickedTank.teamId != 0 && !clickedTank.IsDead)
        {
            foreach (Tank_T90 tank in selectedTanks)
            {
                if (tank == null || tank.IsDead) continue;

                TankCombat combat = tank.GetComponent<TankCombat>();
                if (combat != null) combat.SetTarget(clickedTank);
            }
            return;
        }

        // Правый клик по земле — движение строем.
        int unitCount = selectedTanks.Count;
        int columns = Mathf.CeilToInt(Mathf.Sqrt(unitCount));
        int rows = Mathf.CeilToInt(unitCount / (float)columns);

        for (int i = 0; i < unitCount; i++)
        {
            Tank_T90 tank = selectedTanks[i];
            if (tank == null || tank.IsDead) continue;

            TankCombat combat = tank.GetComponent<TankCombat>();
            if (combat != null) combat.ClearTarget();

            TankMovement movement = tank.GetComponent<TankMovement>();
            if (movement == null) continue;

            int column = i % columns;
            int row = i / columns;
            float offsetX = (column - (columns - 1) * 0.5f) * formationSpacing;
            float offsetY = ((rows - 1) * 0.5f - row) * formationSpacing;
            Vector3 formationOffset = new Vector3(offsetX, offsetY, 0f);

            movement.SetDestination(mouseWorld + formationOffset);
        }
    }

    // ==================== ОТРИСОВКА ИНТЕРФЕЙСА (GUI) ====================
    private void OnGUI()
    {
        // 1. Отрисовка зеленой рамки выделения
        if (isDragging && isBoxSelecting)
        {
            Vector3 startScreen = Camera.main.WorldToScreenPoint(dragStartWorld);
            Vector3 currentScreen = Input.mousePosition;

            startScreen.y = Screen.height - startScreen.y;
            currentScreen.y = Screen.height - currentScreen.y;

            float width = Mathf.Abs(startScreen.x - currentScreen.x);
            float height = Mathf.Abs(startScreen.y - currentScreen.y);
            float x = Mathf.Min(startScreen.x, currentScreen.x);
            float y = Mathf.Min(startScreen.y, currentScreen.y);

            Rect rect = new Rect(x, y, width, height);

            GUI.color = new Color(0f, 1f, 0f, 0.15f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.green;
            GUI.Box(rect, "");
        }

        // 2. Отрисовка линий и радиуса атаки при наведении на врага
        if (hoveredEnemy != null && selectedTanks.Count > 0)
        {
            foreach (var tank in selectedTanks)
            {
                if (tank == null) continue;

                TankCombat combat = tank.GetComponent<TankCombat>();
                if (combat == null) continue;

                DrawAttackLine(tank.transform.position, hoveredEnemy.transform.position, combat.attackRange);
            }
        }

        DrawAmmoHud();
    }

    private void DrawAmmoHud()
    {
        Tank_T90 hudTank = null;
        TankCombat combat = null;

        // При мультивыделении HUD показывает боезапас первого выбранного танка
        // с TankCombat; клавиши при этом применяются ко всем выбранным танкам.
        foreach (Tank_T90 tank in selectedTanks)
        {
            if (tank == null) continue;

            TankCombat candidate = tank.GetComponent<TankCombat>();
            if (candidate == null) continue;

            hudTank = tank;
            combat = candidate;
            break;
        }

        if (combat == null) return;

        const float panelWidth = 188f;
        const float panelHeight = 108f;
        float panelX = 16f;
        float panelY = Screen.height - panelHeight - 16f;
        Rect panel = new Rect(panelX, panelY, panelWidth, panelHeight);

        Color previousColor = GUI.color;
        GUI.color = new Color(0.08f, 0.09f, 0.1f, 0.92f);
        GUI.Box(panel, GUIContent.none);
        GUI.color = Color.white;

        GUI.Label(new Rect(panelX + 10f, panelY + 5f, panelWidth - 20f, 18f), "AMMUNITION");
        GUI.Label(new Rect(panelX + 10f, panelY + 22f, panelWidth - 20f, 16f), hudTank.name);

        DrawAmmoRow(panelX, panelY + 42f, ShellType.AP, "1", combat);
        DrawAmmoRow(panelX, panelY + 62f, ShellType.HEAT, "2", combat);
        DrawAmmoRow(panelX, panelY + 82f, ShellType.HE, "3", combat);

        GUI.color = previousColor;
    }

    private void DrawAmmoRow(float panelX, float rowY, ShellType type, string key, TankCombat combat)
    {
        bool selected = combat.SelectedShell == type;
        Color previousColor = GUI.color;

        if (selected)
        {
            GUI.color = new Color(0.2f, 0.55f, 0.25f, 0.9f);
            GUI.DrawTexture(new Rect(panelX + 6f, rowY, 176f, 19f), Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
        GUI.Label(new Rect(panelX + 12f, rowY, 110f, 19f), key + "  " + type);
        GUI.Label(new Rect(panelX + 132f, rowY, 42f, 19f), combat.GetAmmo(type).ToString());

        GUI.color = previousColor;
    }

    // Рисует линию и крестик радиуса атаки
    private void DrawAttackLine(Vector3 startPos, Vector3 endPos, float attackRange)
    {
        // Переводим мировые координаты в экранные для GUI
        Vector3 startScreen = Camera.main.WorldToScreenPoint(startPos);
        Vector3 endScreen = Camera.main.WorldToScreenPoint(endPos);

        // В GUI ось Y идет сверху вниз
        startScreen.y = Screen.height - startScreen.y;
        endScreen.y = Screen.height - endScreen.y;

        Vector2 start2D = new Vector2(startScreen.x, startScreen.y);
        Vector2 end2D = new Vector2(endScreen.x, endScreen.y);

        float screenDist = Vector2.Distance(start2D, end2D);
        float angle = Mathf.Atan2(end2D.y - start2D.y, end2D.x - start2D.x) * Mathf.Rad2Deg;

        float worldDist = Vector3.Distance(startPos, endPos);

        // Выбираем цвет: Зеленый если враг в зоне видимости, Красный если нет
        Color lineColor = worldDist <= attackRange ? new Color(0f, 1f, 0f, 0.6f) : new Color(1f, 0f, 0f, 0.6f);

        // 1. Рисуем саму линию (поворачиваем текстуру)
        GUI.color = lineColor;
        GUIUtility.RotateAroundPivot(angle, start2D);
        GUI.DrawTexture(new Rect(start2D.x, start2D.y - 1f, screenDist, 2f), Texture2D.whiteTexture);
        GUIUtility.RotateAroundPivot(-angle, start2D); // Возвращаем угол на место

        // 2. Рисуем отметку радиуса атаки (крестик на линии)
        // Если враг ближе радиуса атаки, крестик рисуется прямо на враге
        float clampedDist = Mathf.Min(worldDist, attackRange);

        // Вычисляем, где именно на линии поставить крестик
        Vector3 rangeWorldPoint = startPos + (endPos - startPos).normalized * clampedDist;
        Vector3 rangeScreen = Camera.main.WorldToScreenPoint(rangeWorldPoint);
        rangeScreen.y = Screen.height - rangeScreen.y;

        // Рисуем желтый крестик (отметку ренжи)
        float markSize = 8f;
        GUI.color = Color.yellow;

        // Горизонтальная палочка
        GUI.DrawTexture(new Rect(rangeScreen.x - markSize / 2, rangeScreen.y - 1f, markSize, 2f), Texture2D.whiteTexture);
        // Вертикальная палочка
        GUI.DrawTexture(new Rect(rangeScreen.x - 1f, rangeScreen.y - markSize / 2, 2f, markSize), Texture2D.whiteTexture);
    }
}