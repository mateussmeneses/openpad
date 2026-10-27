# OpenPad 🎮

[![OpenPad CI/CD](https://github.com/mateussmeneses/openpad/actions/workflows/ci.yml/badge.svg)](https://github.com/mateussmeneses/openpad/actions/workflows/ci.yml)
[![Version](https://img.shields.io/badge/version-0.2.0-blue.svg)](https://github.com/mateussmeneses/openpad/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Open-source Gamepad-to-Keyboard/Mouse Remapper for Windows**

OpenPad é uma aplicação Windows moderna e segura para remapear botões de controles (Xbox, PlayStation, genéricos) para teclas de teclado e botões do mouse. Construído com C#/.NET 10, WPF (MVVM), SDL3 e APIs Win32 nativas.

Licenciado sob [MIT License](LICENSE).

---

## 🌟 Recursos e Evoluções (v0.2.0)

- **Troca Automática de Perfis por Jogo (`AutoProfileManager`)**:
  - Monitoramento contínuo em segundo plano do processo em primeiro plano (`GetForegroundWindow`).
  - Associação de perfis a executáveis (ex: `notepad.exe`, `Doom.exe`).
  - Alternância atômica e segura com liberação imediata de quaisquer inputs retidos.
- **Feedback Háptico com Vibração SDL3 (`SDL.RumbleGamepad`)**:
  - Pulso curto e nítido (35.000 de intensidade) ao **Ativar / Armar**.
  - Pulso grave e firme (40.000 de intensidade) ao **Desarmar / Emergência**.
  - Vibração de gatilhos acionados para sensação tátil instantânea.
- **Curvas de Resposta de Analógicos Customizáveis**:
  - Modos configuráveis: **Linear**, **Quadrática** (precisão cirúrgica no centro), **Agressiva** (resposta rápida para ação e FPS) e **Suave** (s-curve para pilotagem/simuladores).
  - Normalização radial e filtragem estrita de zona morta (*deadzone*).
- **Integração com Área de Notificação (System Tray)**:
  - Ícone dinâmico com status de arme (Verde: Armado, Cinza/Vermelho: Desarmado).
  - Opções para minimizar para a bandeja e fechar para a bandeja.
  - Menu de contexto na bandeja com comandos rápidos: Ativar, Desarmar, Abrir Janela e Fechar.
- **Atalhos Globais e Combinação no Controle**:
  - Atalho global de teclado Win32: `Ctrl + Shift + F12` para alternar entre Armar/Desarmar.
  - Atalho físico no controle: Segurar **Back + Start** (ou View + Menu) por 1,2 segundos para alternar o arme com feedback tátil.
- **Detecção Nativa com SDL3**:
  - Suporte completo a gamepads modernos via SDL3 Gamepad API nativa (x64) empacotada.
  - Monitoramento em tempo real a 120 Hz de eixos, botões e gatilhos.
- **Mapeamento Flexível**:
  - Configure qualquer botão do controle para disparar uma tecla do teclado (códigos Win32 Virtual-Key) ou botão do mouse.
- **Perfis Versionados em JSON**:
  - Salve, carregue e restaure perfis com validação de esquema (`schemaVersion`).
- **Segurança e Confiabilidade por Padrão**:
  - Inicia sempre **DESARMADO** (*DISARMED*).
  - **Liberação Garantida de Inputs**: Ao desarmar, desconectar o controle, alternar de perfil ou fechar a janela, todas as teclas virtuais e botões do mouse pressionados são liberados imediatamente via `ReleaseAll()`.
  - Zero drivers proprietários não assinados, zero injeção de processos ou riscos de banimento de anti-cheat.

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
