# OpenPad 🎮

**Open-source Gamepad-to-Keyboard/Mouse Remapper for Windows**

OpenPad é uma aplicação Windows moderna e segura para remapear botões de controles (Xbox, PlayStation, genéricos) para teclas de teclado e botões do mouse. Construído com C#/.NET 10, WPF (MVVM), SDL3 e APIs Win32 nativas.

Licenciado sob [MIT License](LICENSE).

---

## 🌟 Recursos do MVP (v0.1)

- **Detecção Nativa com SDL3**: Suporte completo a gamepads modernos via SDL3 Gamepad API nativa (x64) empacotada.
- **Monitoramento em Tempo Real**: Visualização ao vivo dos botões pressionados, direcionais (D-Pad), gatilhos analógicos e analógicos (sticks).
- **Mapeamento Flexível**: Configure qualquer botão do controle para disparar uma tecla do teclado (códigos Win32 Virtual-Key) ou botão do mouse (Esquerdo, Direito, Meio, Avançar/Voltar).
- **Perfis Versionados em JSON**: Salve, carregue e restaure perfis com validação de esquema (`schemaVersion`), nomes e limites de zonas mortas.
- **Segurança e Confiabilidade por Padrão**:
  - Inicia sempre **DESARMADO** (*DISARMED*). Nenhuma tecla ou clique é enviado até o usuário clicar em **ATIVAR**.
  - **Liberação Garantida de Inputs**: Ao desarmar, desconectar o controle, alternar de perfil ou fechar a janela, todas as teclas virtuais e botões do mouse pressionados são liberados imediatamente via `ReleaseAll()`.
  - Botão de **Emergência: Desarmar** sempre acessível na interface.
- **Limpo e Não Invasivo**:
  - Utiliza `SendInput` do Win32.
  - **Zero** drivers virtuais proprietários ou não assinados no MVP.
  - **Zero** injeção de processos, ganchos globais de teclado (*keylogger*), alteração de memória de jogos ou requisitos de privilégios de Administrador.

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
