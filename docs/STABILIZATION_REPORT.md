# 🛠️ Relatório de Estabilização e Engenharia — OpenPad

**Data da Homologação:** 28/10/2026  
**Branch de Estabilização:** `stabilization/windows-input-reliability`  
**Responsável:** Equipe de Estabilização de Sistemas Windows (Windows Systems Engineer & SDET)

---

## 1. Problemas P0 Identificados e Corrigidos

### 1.1 Win32 SendInput — Validação de Retorno, Recuperação e Liberação Pendente
- **Sintoma / Falha:** `Win32SendInputSink.cs` chamava `SendInput()` sem verificar o valor de retorno inteiro (que indica quantos eventos foram efetivamente inseridos na fila do SO). Além disso, em chamadas a `SendKeyUp` que falhassem por UIPI ou bloqueios do Windows, a classe removia a tecla de `_pressedKeys`, abandonando a tecla fisicamente travada no sistema operacional sem registro nem capacidade de recuperação.
- **Correção Implementada:**
  - `IInputSink` e `Win32SendInputSink` foram atualizados para retornar `bool` em todas as operações de injeção.
  - Implementado rastreamento de liberações pendentes (`PendingReleaseKeys` e `PendingReleaseMouseButtons`). Quando o sistema operacional recusa o evento `KeyUp`, a tecla **permanece** em `_pressedKeys` e é registrada em `PendingReleaseKeys`, com emissão de log diagnóstico estruturado.
  - Criado o método de recuperação `RetryPendingReleases()`, executado reativamente no início de cada ciclo do `MappingEngine`, recuperando as teclas assim que o foco do sistema operacional é restabelecido.
  - Delegate `SendInputNativeHandler` permite injeção nativa simulada em testes unitários.
  - Testes em `Win32SendInputSinkTests.cs` cobrem injeções com sucesso, rejeições com retenção de pendência e recuperação completa via `RetryPendingReleases`.

### 1.2 Modelo de Contagem de Referências por Entrada Física no MappingEngine
- **Sintoma / Falha:** A implementação anterior mantinha apenas um `HashSet<VirtualKey>` por dispositivo. Se dois botões físicos do **mesmo** controle fossem mapeados para a mesma saída (ex: Botão A → Espaço, Botão B → Espaço), ao pressionar A, depois B, e soltar A, o sistema removia Espaço do `HashSet` e liberava prematuramente a tecla, mesmo com B ainda mantido pelo usuário.
- **Correção Implementada:**
  - O `DeviceInputState` foi refatorado para rastrear a origem da entrada física:
    - `ActiveButtonVirtualKeys` / `ActiveButtonMouseButtons` (mapeamento por `GamepadButton`).
    - `ActiveTriggerVirtualKeys` / `ActiveTriggerMouseButtons` (mapeamento por `GamepadAxis`).
  - O contador de referências global (`_virtualKeyRefCounts` e `_mouseButtonRefCounts`) contabiliza a quantidade total de **entradas físicas ativas** (do mesmo dispositivo ou de controles concorrentes) que exigem aquela saída sintética.
  - A saída só é liberada com `SendKeyUp` quando **nenhuma entrada física ativa** exigir mais aquela tecla.
  - Ao desconectar um controle, todas as suas entradas físicas ativas são desassociadas de forma granular, mantendo as entradas de outros jogadores intocadas.
  - O atalho físico `Back + Start` foi isolado por dispositivo (`WasArmComboActive` no `DeviceInputState`), impedindo que snapshots alternados de múltiplos gamepads cancelem a detecção do combo.
  - Testes em `MappingLifecycleTests.cs` validam o cenário reproduzível (A+B para Espaço), combinações de botão e gatilho, e alternância de múltiplos gamepads.

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

### 2.1 Empacotamento Portátil Estrito e Verificação do SDL3
- Target `CopySDL3NativeDll` no `OpenPad.App.csproj` configurado com verificação estrita: em publicações para `win-x64`, a compilação falha imediatamente se `SDL3.dll` não for copiada para o diretório de publicação.
- Pipeline do GitHub Actions configurada para verificar a presença de `SDL3.dll` e `OpenPad.App.exe`, comprimir o artefato portátil `OpenPad-win-x64.zip`, calcular o hash SHA-256 e anexar ambos como artefatos da release.

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
