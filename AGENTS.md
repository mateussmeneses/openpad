# AGENTS.md — OpenPad

Projeto open source MIT. Leia `docs/PRD.md` e `docs/ARCHITECTURE.md` antes de editar.

## Stack
C#/.NET 10, WPF/MVVM, SDL3 native para gamepads, Win32 SendInput para teclado/mouse, perfis JSON locais, NUnit.

## Domínio de entrada
- O app inicia DESARMADO. NENHUM evento de saída até ativação explícita.
- Distinguir botão pressionado/segurado/solto; gerar keydown/keyup em vez de repetir teclas indefinidamente.
- Liberar todos os inputs sintéticos ativos ao desarmar, trocar perfil, desconectar controle ou encerrar.
- Não bloquear thread UI. Leitura SDL3 em worker de ciclo de vida controlado; documentar thread-safety e pump de eventos.
- Normalizar eixos, deadzone e histerese; testar com fontes falsas antes de device real.
- Não injetar processos, não alterar memória de jogo e não ocultar controladores do sistema.
- Sem drivers virtuais, anti-cheat bypass, hooks globais ou permissões admin no MVP.
- Se o dispositivo ou biblioteca não estiver disponível, mostrar estado indisponível sem crashes.

## Dependências
Checar versão, licença e redistribuição do SDL3 antes de instalar; incluir runtime nativo do Windows x64 licenciado corretamente. Prefira mecanismo limpo de resolução de dependência ao copiar DLL aleatória de sites de terceiros.

## Qualidade
Usar `IControllerSource`, `IInputSink`, `IProfileStore` e implementações falsas nos testes; sem engine de remapeamento em code-behind da UI. Validar JSON e versionamento de esquema. Rodar `dotnet build`, `dotnet test` e documentar testes em aparelho real (se feitos).
## Pesquisa com REA (workflow novo)
- Leia `docs/REA_RESEARCH_PLAN.md` antes de realizar pesquisa sobre produtos concorrentes.
- O REA é um **recurso de desenvolvimento**, não uma dependência da aplicação distribuída.
- Antes de escrever funcionalidades inspiradas em outros softwares, registre observações reproduzíveis e um requisito próprio.
- Não copie pseudocódigo decompilado, interfaces exclusivas, textos ou arquivos binários fechados.
- Valide disponibilidade real das ferramentas e documente as limitações.
