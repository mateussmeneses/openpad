# OpenPad 🎮

[![OpenPad CI/CD](https://github.com/mateussmeneses/openpad/actions/workflows/ci.yml/badge.svg)](https://github.com/mateussmeneses/openpad/actions/workflows/ci.yml)
[![Version](https://img.shields.io/badge/version-2.0.0--milestone-brightgreen.svg)](https://github.com/mateussmeneses/openpad/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Tests: 126 Passed](https://img.shields.io/badge/tests-126%20passed-success.svg)](https://github.com/mateussmeneses/openpad/actions)

**Open-source Gamepad-to-Keyboard/Mouse Remapper for Windows — Versão 2.0.0 Major Milestone**

OpenPad é uma suíte profissional, moderna e de alta precisão para remapear botões de controles (Xbox, PlayStation DualSense/DualShock, Nintendo Switch Pro e genéricos) para teclas de teclado, botões do mouse e saídas virtuais. Construído com C#/.NET 10, WPF (MVVM), SDL3 e APIs Win32 nativas.

Licenciado sob [MIT License](LICENSE).

---

## 🚀 Arquitetura e Recursos Completos (v2.0.0 Major Milestone)

### 🧩 1. Suíte de Extensibilidade e Plugins (v2.0.0)
- **Plugin Host Engine (`PluginHostEngine`)**: Arquitetura modular que suporta plugins de terceiros para transformação de input, filtros cinemáticos e saídas personalizadas.
- **Isolamento de Falhas e Quarentena Automática**: Sandboxing resiliente que captura exceções no pipeline sem travar o loop de 1000Hz. Plugins instáveis são postos em quarentena automaticamente após limite configurável de falhas consecutivas.

### 🎯 2. Motores Avançados de Entrada e Resposta (v1.1 - v1.9)
- **Aceleração Balística do Mouse (`MouseAccelerationEngine`)**: Curva exponencial ajustável, multiplicador sniper de precisão e cardinal axis snapping (±7°).
- **Camadas Dinâmicas de Ação (`ActionLayerManager`)**: Alternância de layout (Hold/Toggle) para funções secundárias com fallback em cascata para o perfil base.
- **Sintetizador Turbo de Tiro Rápido (`TurboPulseSynthesizer`)**: Emulação de disparo de alta cadência (1 a 60 Hz) com duty-cycle ajustável e limite de rajada.
- **Flick Stick 360° (`FlickStickEngine`)**: Giro instantâneo de câmera para jogos de tiro competitivo com suavização anti-screen-tearing em 80ms.
- **Monitor de Telemetria de Bateria (`BatteryStatusMonitor`)**: Nível de carga sem fio (0-100%), detecção de carregamento e avisos preditivos de bateria baixa/crítica.
- **Motor de Acordes / Combos (`ChordingEngine`)**: Reconhecimento de acordes simultâneos (ex: A+B) com janela de tolerância microtemporal de 45ms.
- **Modelador de Formas de Deadzone (`DeadzoneShapeMorpher`)**: Suporte a zonas mortas Radiais, Axiais e Quadradas com injeção de Anti-Deadzone e projeção quadrada diagonal (1.0, 1.0).
- **Sintetizador Háptico por Áudio (`AudioHapticSynthesizer`)**: Conversão direta de espectro FFT e ondas sonoras PCM em vibração de motores duplos (Large/Small Motor).
- **Display Overlay em Tela (`OnScreenDisplayOverlay`)**: Renderização HUD in-game com indicador de perfil ativo, camada, bateria, frequência (Hz), latência (ms) e notificações toast.

### 🛡️ 3. Recursos Principais de Segurança e Sistema (v1.0)
- **Troca Automática por Janela (`AutoProfileManager`)**: Alternância contextual atômica ao focar executáveis associados.
- **Mira Giroscópica (`GyroAimingFilter`)**: Conversão precisa de giroscópio para mouse com retenção de frações sub-pixel.
- **Menu Radial / Weapon Wheel (`RadialMenuEngine`)**: Menu virtual transparente de 4 a 8 fatias para troca rápida de armas.
- **Hair-Trigger de Dois Estágios (`DualActionTriggerEngine`)**: Ação suave e ação total com limiares analógicos customizados.
- **Gravador e Reprodutor de Macros (`MacroEngine`)**: Sequências temporizadas com cancelamento atômico seguro.
- **Padrões de Vibração Háptica (`HapticPatternPlayer`)**: Presets tácteis personalizáveis.
- **Guardião de Segurança de Processos (`ProcessSecurityGuard`)**: Bloqueio ativo contra janelas do sistema (Taskmgr, UAC, gerenciadores de senhas).
- **Compartilhamento de Perfis com SHA-256 (`ProfileBundleHub`)**: Integridade criptográfica à prova de adulterações.
- **Suporte a 4 Controles em Hot-Plug (`GamepadSlotManager`)**: Mapeamento simultâneo para P1 a P4.
- **Modo Linha de Comando Headless (`CliRunner`)**: Execução automatizada e silenciosa via terminal.

---

## 🛠️ Tecnologias
- **Plataforma:** .NET 10 (C# 13, Windows 10/11 x64)
- **Interface Gráfica:** WPF com MVVM
- **Camada de Entrada:** SDL3 Nativo (DirectInput, XInput, HID)
- **Testes Unitários:** NUnit 4 (126 testes automatizados)
- **Integração Contínua:** GitHub Actions CI/CD (.NET 10 Runner)

---

## 📦 Como Compilar e Testar

```bash
# Restaurar dependências
dotnet restore

# Compilar com avisos tratados como erro
dotnet build --warnaserror

# Executar suíte completa de testes
dotnet test --nologo
```
