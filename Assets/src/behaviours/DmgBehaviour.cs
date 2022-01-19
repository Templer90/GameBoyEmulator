using System;
using src.adapters;
using src.gameboy;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;


public class DmgBehaviour : MonoBehaviour
{
    public Object rom;
    public SpriteRenderer spriteRenderer;

    private GameBoy _gameboy;
    private DirectBitMap _bitmap;
    private Joypad _joypad;

    private GameBoyEmulator _controls;

    void Awake()
    {
        _controls = new GameBoyEmulator();
    }

    void OnEnable()
    {
        _controls.asset.Enable();
    }

    void OnDisable()
    {
        _controls.asset.Disable();
    }

    void Start()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        _bitmap = new DirectBitMap(spriteRenderer);
        _joypad = new Joypad();
        InitControls(_controls,_joypad);
        _gameboy = new GameBoy(_bitmap, _joypad, AssetDatabase.GetAssetPath(rom));
    }

    // Update is called once per frame
    void Update()
    {
        _gameboy.Tick();
        _bitmap.CommitData();
    }

    private static void InitControls(GameBoyEmulator controls, Joypad joypad)
    {
        void MoveAction(InputAction.CallbackContext ctx)
        {
            var vec = ctx.ReadValue<Vector2>();

            joypad.HandleKeyAction(Joypad.Keys.RIGHT, (vec.x > 0.5));
            joypad.HandleKeyAction(Joypad.Keys.LEFT, (vec.x < -0.5));
            joypad.HandleKeyAction(Joypad.Keys.UP, (vec.y > 0.5));
            joypad.HandleKeyAction(Joypad.Keys.DOWN, (vec.y < -0.5));
        }
        
        controls.Player.Move.performed += MoveAction;
        controls.Player.Move.canceled += MoveAction;

        controls.Player.Start.performed += ctx => joypad.HandleKeyAction(Joypad.Keys.START,true);
        controls.Player.Start.canceled += ctx => joypad.HandleKeyAction(Joypad.Keys.START,false);

        controls.Player.Select.performed += ctx => joypad.HandleKeyAction(Joypad.Keys.SELECT,true);
        controls.Player.Select.canceled += ctx => joypad.HandleKeyAction(Joypad.Keys.SELECT,false);

        controls.Player.A.performed += ctx => joypad.HandleKeyAction(Joypad.Keys.A,true);
        controls.Player.A.canceled += ctx => joypad.HandleKeyAction(Joypad.Keys.A,false);

        controls.Player.B.performed += ctx => joypad.HandleKeyAction(Joypad.Keys.B,true);
        controls.Player.B.canceled += ctx => joypad.HandleKeyAction(Joypad.Keys.B,false); 
    }
}