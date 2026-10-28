# OpenPad 🎮

[![OpenPad CI/CD](https://github.com/mateussmeneses/openpad/actions/workflows/ci.yml/badge.svg)](https://github.com/mateussmeneses/openpad/actions/workflows/ci.yml)
[![Version](https://img.shields.io/badge/version-3.0.0--milestone-brightgreen.svg)](https://github.com/mateussmeneses/openpad/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Tests: 174 Passed](https://img.shields.io/badge/tests-174%20passed-success.svg)](https://github.com/mateussmeneses/openpad/actions)

**Open-source Gamepad-to-Keyboard/Mouse Remapper for Windows — Versão 3.0.0 Major Milestone**

OpenPad é uma suíte profissional, moderna e de alta precisão para remapear botões de controles (Xbox, PlayStation DualSense/DualShock, Nintendo Switch Pro e genéricos) para teclas de teclado, botões do mouse e saídas virtuais. Construído com C#/.NET 10, WPF (MVVM), SDL3 e APIs Win32 nativas.

Licenciado sob [MIT License](LICENSE).

---

## 🚀 Arquitetura e Recursos Completos (v3.0.0 Major Milestone)

### ⚡ 1. Microkernel de Entrada de Zero Latência (v3.0.0)
- **OpenPad Microkernel (`OpenPadMicrokernel`)**: Arquitetura desacoplada de ultra-alta frequência com ring-buffer lock-free, streaming de pacotes IPC para isolamento de privilégios e latência sub-milissegundo.
- **Heartbeat & Watchdog Fail-Safe**: Monitoramento contínuo de cadência de ticks com transição automática para modo de segurança (`SafeMode` / `Degraded`) e fallback em caso de anomalia de agendamento do kernel.

### 🌐 2. Camada de Federação, Diagnóstico e Nuvem (v2.7 - v2.9)
- **Profile Cloud Sync & Security Audit (`ProfileCloudSyncEngine`)**: Serialização com assinaturas criptográficas HMAC-SHA256, verificação à prova de adulteração e sandbox anti-alvo do sistema operacional.
- **Game Library Scanner (`GameLibraryScanner`)**: Detecção automática de bibliotecas de jogos Steam (VDF manifests), Epic Games Store e Xbox Game Pass para auto-provisionamento de perfis.
- **Stick Circularity Benchmark (`StickCircularityBenchmark`)**: Diagnóstico de hardware com medição de erro de circularidade, raio de drift de repouso e grading automático de precisão (A a D).

### 🕹️ 3. Sintetizadores de Hardware & Feedback Háptico (v2.1 - v2.6)
- **Curvas de Atuação de Gatilho Hairpin (`TriggerActuationCurveEngine`)**: Gatilhos digitais imediatos e curvas parabólicas e dual-stage com zonas mortas de pré-curso.
- **Motor de Gravação e Replay de Entrada (`InputReplayEngine`)**: Gravação frame-accurate e playback determinístico de macros analógicas com aborto instantâneo por override físico.
- **Sintetizador de Gatilhos Adaptativos (`AdaptiveTriggerSynthesizer`)**: Emulação avançada de efeitos de resistência DualSense (Feedback, Armas, Vibração contínua e Tensão elástica).
- **Gesto de Touchpad Capacitivo (`TouchpadGestureEngine`)**: Gestos multitoque (Taps, Swipes direcionais, Pinch-to-zoom) mapeados para ações de teclado e mouse.
- **Compensador de Recuo FPS (`RecoilCompensator`)**: Mitigação orgânica de dispersão de tiro vertical e horizontal com atraso de subida e micro-jitter humanizado.
- **Ponte de Drivers Virtuais ViGEmBus (`VirtualBusDriverBridge`)**: Emulação de controles virtuais Xbox 360 e DualShock 4 com loopback de vibração.

### 🧩 4. Suíte de Extensibilidade e Plugins (v2.0.0)
- **Plugin Host Engine (`PluginHostEngine`)**: Arquitetura modular que suporta plugins de terceiros para transformação de input, filtros cinemáticos e saídas personalizadas.
- **Isolamento de Falhas e Quarentena Automática**: Sandboxing resiliente que captura exceções no pipeline sem travar o loop de 1000Hz. Plugins instáveis são postos em quarentena automaticamente após limite configurável de falhas consecutivas.

### 🎯 5. Motores Avançados de Entrada e Resposta (v1.1 - v1.9)
- **Aceleração Balística do Mouse (`MouseAccelerationEngine`)**: Curva exponencial ajustável, multiplicador sniper de precisão e cardinal axis snapping (±7°).
- **Camadas Dinâmicas de Ação (`ActionLayerManager`)**: Alternância de layout (Hold/Toggle) para funções secundárias com fallback em cascata para o perfil base.
- **Sintetizador Turbo de Tiro Rápido (`TurboPulseSynthesizer`)**: Emulação de disparo de alta cadência (1 a 60 Hz) com duty-cycle ajustável e limite de rajada.
- **Flick Stick 360° (`FlickStickEngine`)**: Giro instantâneo de câmera para jogos de tiro competitivo com suavização anti-screen-tearing em 80ms.
- **Monitor de Telemetria de Bateria (`BatteryStatusMonitor`)**: Nível de carga sem fio (0-100%), detecção de carregamento e avisos preditivos de bateria baixa/crítica.
- **Motor de Acordes / Combos (`ChordingEngine`)**: Reconhecimento de acordes simultâneos (ex: A+B) com janela de tolerância microtemporal de 45ms.
- **Modelador de Formas de Deadzone (`DeadzoneShapeMorpher`)**: Suporte a zonas mortas Radiais, Axiais e Quadradas com injeção de Anti-Deadzone e projeção quadrada diagonal (1.0, 1.0).
- **Sintetizador Háptico por Áudio (`AudioHapticSynthesizer`)**: Conversão direta de espectro FFT e ondas sonoras PCM em vibração de motores duplos (Large/Small Motor).
- **Display Overlay em Tela (`OnScreenDisplayOverlay`)**: Renderização HUD in-game com indicador de perfil ativo, camada, bateria, frequência (Hz), latência (ms) e notificações toast.

### 🛡️ 6. Recursos Principais de Segurança e Sistema (v1.0)
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
- **Testes Unitários:** NUnit 4 (174 testes automatizados)
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
