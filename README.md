# OpenPad 🎮

[![OpenPad CI/CD](https://github.com/mateussmeneses/openpad/actions/workflows/ci.yml/badge.svg)](https://github.com/mateussmeneses/openpad/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Platform: Windows x64](https://img.shields.io/badge/platform-Windows%20x64-blue.svg)](https://github.com/mateussmeneses/openpad)

**Remapeador Open-Source de Controles para Teclado e Mouse no Windows**

OpenPad é uma alternativa gratuita e confiável para remapear controles físicos (Xbox, PlayStation DualSense/DualShock, Nintendo Switch Pro e genéricos DirectInput/XInput) para teclas de teclado e cliques/movimentos de mouse. Construído em C# / .NET 10, WPF (MVVM), SDL3 e Win32 SendInput nativo.

Licenciado sob [MIT License](LICENSE).

---

## 🏷️ Matriz de Prontidão e Classificação de Funcionalidades

De acordo com as diretrizes de transparência e engenharia do projeto, cada componente é rigorosamente classificado quanto ao seu status real:

| Funcionalidade | Status | Descrição e Evidência |
|---|:---:|---|
| **Leitura de Gamepads via SDL3** | `VERIFIED` | Detecção hot-plug de controles via SDL3 Gamepad API em thread dedicada sem engasgos na UI. |
| **Injeção de Teclado/Mouse Win32** | `VERIFIED` | Envio de `SendInput` com validação de retorno, detecção de bloqueio UIPI e liberação garantida (`ReleaseAll`). |
| **Isolamento Multi-Controle** | `VERIFIED` | Estados isolados por dispositivo com contagem de referências; soltar botão em um controle não desativa entrada do outro. |
| **Ciclo de Vida do Remapeamento** | `VERIFIED` | 10 passos de ciclo de vida homologados: armar, desarmar, desconectar, trocar perfil e encerrar sem inputs presos. |
| **Perfis de Mapeamento JSON** | `VERIFIED` | Serialização e carregamento com fallback seguro e validação de schema. |
| **Desarme de Emergência** | `VERIFIED` | Atalho físico no gamepad (Back + Start) e liberação atômica global. |
| **Pacote Portátil Windows x64** | `VERIFIED` | Script de build com cópia automática da `SDL3.dll` nativa ao lado do executável. |
| **Microkernel de Eventos (Ring-Buffer)** | `EXPERIMENTAL` | Fila circular lock-free em memória e watchdog local (componente de pesquisa). |
| **Emulação Virtual Xbox / DS4** | `EXPERIMENTAL` | Estruturas de dados preparadas para ViGEmBus (fora do MVP por exigir driver de kernel terceiro). |
| **Sincronização de Perfis em Nuvem** | `EXPERIMENTAL` | Assinatura HMAC-SHA256 local para perfis (backend remoto em planejamento). |
| **Overlay HUD em Tela (OSD)** | `EXPERIMENTAL` | Modelo de dados para HUD (sem injeção direta em swapchain DirectX/Vulkan). |

---

## 🛠️ Stack Tecnológica
- **Plataforma:** .NET 10 (C# 13, Windows 10/11 x64)
- **Interface Gráfica:** WPF com padrão MVVM
- **Camada de Entrada:** SDL3 Nativo (DirectInput, XInput, HID)
- **Camada de Saída:** Win32 `SendInput` com monitoramento de erros de privilégio
- **Testes Automatizados:** 182 testes unitários e de ciclo de vida aprovados no NUnit (100% de sucesso)
- **Compilação:** `dotnet build --warnaserror` (0 Avisos, 0 Erros)

---

## 📦 Como Compilar e Executar

```powershell
# Restaurar dependências
dotnet restore OpenPad.slnx

# Compilar solução em Release com avisos como erro
dotnet build OpenPad.slnx --configuration Release --warnaserror

# Executar suíte completa de testes
dotnet test OpenPad.slnx --configuration Release --no-build

# Publicar pacote portátil x64
dotnet publish src/OpenPad.App/OpenPad.App.csproj --configuration Release -o publish/
```

---

## 📚 Documentação Técnica
- [Arquitetura do Sistema](docs/ARCHITECTURE.md)
- [Relatório de Estabilização e Correções](docs/STABILIZATION_REPORT.md)
- [Roteiro de Homologação Manual](docs/MANUAL_TEST_PLAN.md)
- [Critérios de Prontidão para Distribuição](docs/RELEASE_READINESS.md)
- [Roadmap de Evolução](docs/ROADMAP.md)
