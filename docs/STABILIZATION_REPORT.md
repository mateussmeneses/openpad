# 🛠️ Relatório de Estabilização e Engenharia — OpenPad

**Data da Homologação:** 28/10/2026  
**Branch de Estabilização:** `stabilization/windows-input-reliability`  
**Responsável:** Equipe de Estabilização de Sistemas Windows (Windows Systems Engineer & SDET)

---

## 1. Problemas P0 Identificados e Corrigidos

### 1.1 Win32 SendInput — Validação de Retorno e Tratamento de Erros
- **Sintoma / Falha:** `Win32SendInputSink.cs` chamava `SendInput()` sem verificar o valor de retorno inteiro (que indica quantos eventos foram efetivamente inseridos na fila do sistema operacional). Em caso de bloqueio por integridade (UIPI), permissões ou tela de segurança do Windows, a API retornava 0, mas a classe inseria a tecla em `_pressedKeys` cegamente, corrompendo o estado interno. Além disso, não era possível testar falhas de injeção unitariamente.
- **Correção Implementada:**
  - Criado o delegate `SendInputNativeHandler`, permitindo injeção do chamador nativo para testes e simulação de falhas.
  - Implementada verificação estrita: se `sent == 0`, o erro do Windows é capturado via `Marshal.GetLastWin32Error()`, o contador `FailedEventCount` é incrementado, a falha é registrada nos diagnósticos e a tecla **não** é adicionada a `_pressedKeys`.
  - Em `SendKeyUp`, a tecla é removida de `_pressedKeys` incondicionalmente, garantindo que o estado não retenha teclas fictícias.
  - Criada a suíte `tests/OpenPad.Tests/Win32SendInputSinkTests.cs` simulando retornos com sucesso e retornos com recusa de injeção (0).

### 1.2 Isolamento de Múltiplos Controles no MappingEngine
- **Sintoma / Falha:** O `MappingEngine` mantinha um único `_activeDeviceId` e dicionários globais de botões anteriores (`_previousButtonStates`). Quando dois controles estavam conectados, o snapshot do Controle 2 sobrescrevia o estado do Controle 1, fazendo com que a soltura de um botão no Controle 2 gerasse um evento `KeyUp` que desativava a tecla que o Controle 1 ainda estava pressionando. Além disso, a desconexão de um controle liberava inputs globais de todos os jogadores.
- **Correção Implementada:**
  - Criada a classe interna `DeviceInputState`, encapsulando os estados de botões, gatilhos analógicos, turbo e teclas seguradas individualmente para cada controle conectado.
  - Implementada contagem de referências (`_virtualKeyRefCounts` e `_mouseButtonRefCounts`) para quando múltiplos controles possuem botões mapeados para a mesma tecla de saída.
  - O evento `KeyDown` só é emitido no primeiro acionamento; solturas intermediárias de outros controles apenas decrementam o contador de referências. O `KeyUp` só é emitido quando o último controle solta o botão.
  - Ao desconectar um controle com múltiplos controles ativos, apenas os inputs do dispositivo desconectado são liberados, preservando os demais jogadores.
  - Caso todos os controles sejam desconectados, executa-se a limpeza segura com `ReleaseAll()`.
  - Criada a suíte `tests/OpenPad.Tests/MappingLifecycleTests.cs` comprovando o isolamento de múltiplos controles e a contagem de referências.

### 1.3 Homologação do Ciclo de Vida do Remapeamento
- **Sintoma / Falha:** Ausência de testes automatizados sistemáticos validando o ciclo de vida de 10 passos estipulado nos critérios de aceitação.
- **Correção Implementada:**
  - Adicionado o teste `Lifecycle_TenStepSequence_ExecutesDeterministically` cobrindo rigorosamente:
    1. Conectar controle
    2. Armar motor
    3. Pressionar botão mapeado
    4. Confirmar evento de saída
    5. Soltar botão
    6. Confirmar liberação
    7. Desarmar com botão pressionado (inputs liberados)
    8. Desconectar durante entrada ativa (inputs do dispositivo liberados)
    9. Trocar perfil durante entrada ativa (inputs liberados)
    10. Encerrar aplicação / Dispose (todos os inputs liberados)

---

## 2. Melhorias P1 e Empacotamento Windows

### 2.1 Empacotamento Nativo do SDL3
- Adicionado target automático no `OpenPad.App.csproj` para copiar `SDL3.dll` da pasta nativa x64 (`runtimes/win-x64/native/SDL3.dll`) diretamente para a raiz do pacote portátil publicado em `publish/`.
- Permite que o usuário final execute `OpenPad.App.exe` sem necessidade de instalar dependências externas ou drivers de terceiros.

---

## 3. Reclassificação P2 — Componentes Experimentais

Componentes que requerem drivers de kernel terceiros ou serviços remotos foram explicitamente reclassificados como `EXPERIMENTAL`:
- `VirtualBusDriverBridge` (emulação virtual Xbox/DS4 sem driver ViGEmBus instalado no sistema).
- `OpenPadMicrokernel` (ring-buffer em memória de processo, sem execução em ring-0).
- `ProfileCloudSyncEngine` (assinatura HMAC local sem servidor de nuvem).
- `OnScreenDisplayOverlay` (modelo HUD sem injeção em DirectX/Vulkan).

---

## 4. Métricas e Resultados de Testes

- **Comandos Executados:** `dotnet build OpenPad.slnx --warnaserror`, `dotnet test OpenPad.slnx`
- **Resultados de Testes:** **182 testes unitários e de integração aprovados (0 falhas)**
- **Compilação:** **0 avisos, 0 erros**
