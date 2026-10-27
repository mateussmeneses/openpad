# OpenPad 🎮

[![OpenPad CI/CD](https://github.com/mateussmeneses/openpad/actions/workflows/ci.yml/badge.svg)](https://github.com/mateussmeneses/openpad/actions/workflows/ci.yml)
[![Version](https://img.shields.io/badge/version-1.0.0--stable-brightgreen.svg)](https://github.com/mateussmeneses/openpad/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Tests: 72 Passed](https://img.shields.io/badge/tests-72%20passed-success.svg)](https://github.com/mateussmeneses/openpad/actions)

**Open-source Gamepad-to-Keyboard/Mouse Remapper for Windows — Versão 1.0 Estável**

OpenPad é uma aplicação Windows profissional, moderna e segura para remapear botões de controles (Xbox, PlayStation DualSense/DualShock, Nintendo Switch Pro e genéricos) para teclas de teclado e botões do mouse. Construído com C#/.NET 10, WPF (MVVM), SDL3 e APIs Win32 nativas.

Licenciado sob [MIT License](LICENSE).

---

## 🌟 Recursos Completos (v1.0.0 Estável)

1. **Troca Automática de Perfis por Jogo (`AutoProfileManager`)**:
   - Monitoramento em segundo plano do processo ativo em primeiro plano (`GetForegroundWindow`).
   - Associação de perfis com jogos (ex: `Doom.exe`, `EldenRing.exe`, `notepad.exe`).
   - Transição atômica e segura com liberação imediata de quaisquer inputs retidos.
2. **Filtro de Mira Giroscópica (`GyroAimingFilter`)**:
   - Conversão de velocidade angular do giroscópio (graus/segundo) para deslocamento suave de cursor.
   - Zona morta configurável e acumulador contínuo de frações sub-pixel para micro-mira cirúrgica.
3. **Menu Radial Virtual / Roda de Ações (`RadialMenuEngine`)**:
   - Menu transparente em setores angulares (4 a 8 itens) controlado pelo analógico para troca rápida de armas/feitiços.
4. **Gatilhos de Ação Dupla Hair-Trigger (`DualActionTriggerEngine`)**:
   - Dois estágios de acionamento analógico: Soft Pull (leve) e Full Pull (completo) com modos configuráveis.
5. **Gravador e Reprodutor de Macros (`MacroEngine`)**:
   - Sequências temporizadas de teclas e cliques com precisão de milissegundos e repetição programada.
   - Cancelamento atômico e seguro ao desarmar.
6. **Reprodutor de Padrões Hápticos (`HapticPatternPlayer`)**:
   - Vibração dinâmica com presets (`Heartbeat`, `DoublePulse`, `WarningThud`) via SDL3 Rumble.
7. **Curvas de Resposta de Analógicos Customizáveis**:
   - Modos **Linear**, **Quadrática**, **Agressiva** e **Suave (S-Curve)** com deadzone radial rigorosa.
8. **Guardião de Segurança de Processos (`ProcessSecurityGuard`)**:
   - Bloqueio automático de injeção em janelas críticas do sistema (Gerenciador de Tarefas, prompt UAC, KeePass, 1Password, Bitwarden).
9. **Hub de Compartilhamento de Perfis com SHA-256 (`ProfileBundleHub`)**:
   - Empacotamento de perfis em arquivos `.json` assinados com hash SHA-256 à prova de adulteração.
10. **Gerenciador de Múltiplos Controles e Hot-Plug (`GamepadSlotManager`)**:
    - Suporte a 4 jogadores independentes (P1 a P4) com fallback dinâmico de jogador primário.
11. **Modo Linha de Comando Headless (`CliRunner`)**:
    - Execução e automação direta via terminal: `openpad --profile "FPS.json" --arm --tray`.
12. **Bandeja do Sistema (System Tray) e Atalhos Globais**:
    - Minimizar/fechar para o relógio do Windows, atalho global `Ctrl + Shift + F12` e combo `Back + Start` no controle.
13. **Segurança e Confiabilidade por Padrão**:
    - Inicia sempre **DESARMADO**.
    - Liberação atômica imediata (`ReleaseAll()`) em qualquer interrupção.
    - Zero drivers proprietários não assinados, zero injeção de processos ou riscos de anti-cheat.

---

## 🏗 Arquitetura

```
OpenPad
├── src/OpenPad.Core       # Modelos de domínio, interfaces e máquina de estados pura (MappingEngine)
├── src/OpenPad.Input      # Implementação SDL3 (SDL3ControllerSource) e Win32 SendInput (Win32SendInputSink)
├── src/OpenPad.App        # Interface gráfica WPF em MVVM com tema escuro e monitor ao vivo
└── tests/OpenPad.Tests    # Testes unitários NUnit com doubles (FakeControllerSource e RecordingInputSink)
```

### Princípios de Engenharia
1. **Desacoplamento Completo**: O `MappingEngine` independe de UI e hardware. Ele opera através de `IControllerSource` e `IInputSink`, permitindo testes unitários determinísticos de transições e máquina de estados.
2. **Proteção da Thread UI**: O polling de eventos e estados do controle roda em uma thread worker a ~120 Hz, despachando atualizações visuais para o Dispatcher com throttling (evitando engasgos na UI).
3. **Persistência Segura**: `JsonProfileStore` realiza validação estrita antes de gravar e após carregar arquivos de perfil JSON.

---

## 🚀 Como Executar

### Pré-requisitos
- Windows 10 ou 11 (x64)
- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0)

### 1. Compilar a Solução
```powershell
dotnet build OpenPad.slnx
```

### 2. Executar os Testes Unitários
```powershell
dotnet test OpenPad.slnx
```

### 3. Iniciar o Aplicativo
```powershell
dotnet run --project src/OpenPad.App/OpenPad.App.csproj
```

---

## 🕹 Compatibilidade e Limitações

- O OpenPad utiliza a API oficial `SendInput` da Microsoft para injetar eventos sintéticos de entrada.
- Alguns jogos com sistemas anti-cheat proprietários ou modo exclusivo de *Raw Input* podem ignorar eventos de `SendInput`. Isso é esperado pelo design de não-invasão do OpenPad (não realizamos injeção de DLL nem bypass de anti-cheat).

---

## 📄 Licença

OpenPad é software livre sob licença [MIT](LICENSE).
Bibliotecas nativas de terceiros:
- SDL3 sob licença [zlib](https://github.com/libsdl-org/SDL/blob/main/LICENSE.txt).
