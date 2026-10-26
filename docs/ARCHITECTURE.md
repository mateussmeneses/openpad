# Arquitetura v0.1 — OpenPad

```
WPF App (MVVM)
  └─ App orchestration / armed state
       ├─ IControllerSource → SDL3ControllerSource (native dll)
       ├─ MappingEngine → press/hold/release + deadzone
       ├─ IInputSink → Win32SendInputSink
       └─ IProfileStore → Versioned JSON

Tests → FakeControllerSource + RecordingInputSink
```

## Estado e ciclo de vida
- Input físico deve virar eventos semânticos (pressed/released/axis changed), desacoplados da API SDL3.
- `MappingEngine` é puro e testável; mantém conjunto de teclas/botões sintéticos atualmente pressionados.
- `Disarm()` e `Dispose()` sempre liberam outputs pendentes (try/finally onde necessário).
- Desconexão e troca de perfil disparam release antes de qualquer outro evento.
- Uma thread worker coleta device state e publica snapshots imutáveis; UI usa Dispatcher com throttling, nunca roda polling do dispositivo na thread visual.
- Entrada SDL3: preferir SDL3 Gamepad API; configurar descoberta e inicialização conforme versão fixada.
- Saída: API `SendInput` do Win32. Não chamar WriteProcessMemory ou injetar no processo do jogo.
- JSON deve ter `schemaVersion`, `profileName`, `deviceGuid?`, `bindings` e limites de tamanho/validação.

## Licenças e futuro
SDL3 tem licença zlib; conferir redistribuição e dependências do binding. OpenPad sob MIT com código próprio. A emulação de controle virtual é um módulo futuro independente de driver; não integrar ViGEmBus arquivado nem driver pago silenciosamente.

## Dica para testar
O teste manual inicial deve usar Notepad ou uma janela de teste própria com inputs visíveis. O jogo não é pré-requisito para validar transições de teclado/mouse.
