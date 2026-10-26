# Roadmap — OpenPad

## Sprint 1 — entrada, motor, saída
- [ ] Solution WPF + Core + Input + NUnit
- [ ] Contratos IControllerSource / IInputSink / IProfileStore
- [ ] Backend SDL3 para detecção e leitura de gamepads
- [ ] UI com controles conectados e painel de input ao vivo
- [ ] MappingEngine com button down/up e toggle desarmado por padrão
- [ ] Win32 SendInput para tecla e mouse com release seguro
- [ ] Persistência JSON versionado e testes com fakes
- [ ] Rodar com controle físico Xbox/PlayStation se disponível

## Sprint 2 — UX que diferencia
- [ ] Editor visual arrastar/selecionar botões
- [ ] Deadzones configuráveis, curva do analógico e mouse via stick
- [ ] Perfis por aplicativo com consentimento claro
- [ ] Diagnóstico de conexão Bluetooth/USB e feedback do input

## Sprint 3 — release público
- [ ] CI Windows `dotnet test` e build release
- [ ] Empacotamento portable e checksum
- [ ] README em inglês e português, demo GIF, changelog
- [ ] CONTRIBUTING, issue templates, licença de terceiros, security policy

## Pesquisa futura (não promessa)
- [ ] Gyro, rumble, combinações e macros
- [ ] Emulação de controle virtual com backend mantenível, licenças e assinatura adequadas
- [ ] Verificar nome de marca/repositório OpenPad

## Fontes
- https://wiki.libsdl.org/SDL3/CategoryGamepad
- https://github.com/AntiMicroX/antimicrox
- https://github.com/nefarius/ViGEmBus
- https://github.com/LizardByte/libvirtualhid
