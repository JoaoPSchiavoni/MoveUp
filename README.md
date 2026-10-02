# MoveUp | API de treinos e histórico

API REST em ASP.NET Core 10 para fichas de treino, sessões e acompanhamento de
constância. O MoveUp compartilha este back-end com o
[aplicativo Flutter](https://github.com/JoaoPSchiavoni/MoveUpApp), que também
funciona com armazenamento local no celular.

![Apresentação do MoveUp](MoveUp.png)

## O projeto

Este repositório contém o serviço HTTP e seu banco SQLite no servidor. Ele
oferece catálogo e fichas de exercícios, registro de séries e histórico de
sessões, calendário e métricas de consistência. Entity Framework Core e
migrations mantêm o esquema do banco versionado. Os dados registrados são
preservados como snapshots para que mudanças futuras em uma ficha não alterem
o histórico do atleta.

O serviço destina-se a desenvolvimento e uso local neste estágio. Ele ainda não
implementa contas, autenticação ou separação dos dados por usuário. O app usa
SQLite local por padrão; conectar o app a esta API é opcional. Consulte o
[Front-end](https://github.com/JoaoPSchiavoni/MoveUpApp) para o aplicativo e
sua [arquitetura e modos de armazenamento](https://github.com/JoaoPSchiavoni/MoveUpApp/blob/main/docs/local-product.md).

## Executar localmente

Requisitos: .NET 10 SDK. Na raiz do projeto:

```sh
cp .env.example .env # Na primeira configuração
./scripts/run-local.sh
```

A API atende em `http://localhost:5013`. A inicialização aplica migrations
pendentes e insere o catálogo inicial quando necessário. Verifique o serviço em
`/health`; em Development, consulte o contrato OpenAPI em `/openapi/v1.json`.
Os detalhes de conexão do aplicativo estão na seção [Executar](#executar).

> `.env` e bancos locais não pertencem ao repositório. O `.env.example` contém
> somente configurações de exemplo; mantenha segredos no ambiente privado.

## Arquitetura

```text
MoveUp Flutter → API REST → Controllers → Services → Repositories → SQLite
```

As entidades `Treino`, `Exercicio` e `TreinoExercicio` compõem o catálogo e as
fichas. Sessões mantêm uma cópia independente do treino feito; os serviços
coordenam validação e transações, e os repositórios acessam SQLite.

## Principais recursos

- API para consultar, criar, editar e excluir fichas e exercícios.
- Sessões idempotentes, registro de séries, pausa, retomada e histórico paginado.
- Calendário, rotina, dias planejados, fuso horário e métricas de consistência.
- Migrations incrementais e regras de integridade com SQLite.
- Testes de integração com banco isolado e SQLite real.

## Arquitetura e limite desta entrega

Flutter → ApiWorkoutGateway → Controllers → Services → Repositories → SQLite.

O SQLite fica **na máquina da API**, em `App_Data/moveup.db`. O modo remoto do app precisa de
conexão com a API. O Flutter usa Drift/SQLite no aparelho por padrão, com
backup e importação da API; veja a [arquitetura local do app](https://github.com/JoaoPSchiavoni/MoveUpApp/blob/main/docs/local-product.md). Este projeto
continua sendo o armazenamento do servidor. A API é de uso local/de desenvolvimento,
sem autenticação ou separação de dados por usuário nesta fase.

## Executar

Na pasta MoveUp:

```sh
cp .env.example .env # Apenas na primeira configuração
./scripts/run-local.sh
```

A API atende em `http://localhost:5013`. A inicialização aplica as migrations pendentes, cria o banco e insere os
12 exercícios do catálogo inicial quando necessário. Inicializações seguintes
não duplicam nem sobrescrevem o catálogo. Alterações futuras de esquema devem
usar novas migrations, sem apagar o banco.

Para configurar o arquivo, use `Database__Path=/caminho/moveup.db` no ambiente.
O caminho relativo é resolvido a partir da pasta do projeto. O arquivo antigo
`fitnessapp.db`, se existir na pasta local do sistema, não é alterado ou importado
automaticamente. Este projeto usa um novo banco versionado por migrations;
bancos antigos criados com EnsureCreated precisam de migração de dados planejada.

No projeto Flutter:

```sh
flutter run -d chrome --dart-define=USE_REMOTE_API=true --web-hostname localhost --web-port 5173
```

O app usa banco local por padrão. Ao ativar `USE_REMOTE_API=true`, usa esta API. No emulador Android, o endereço padrão é
`http://10.0.2.2:5013`; nas outras plataformas, `http://localhost:5013`.
Para endereço diferente:

```sh
flutter run --dart-define=USE_REMOTE_API=true --dart-define=API_BASE_URL=https://seu-host
```

Para aparelho físico, configure uma API acessível ao aparelho; localhost é o
próprio dispositivo. HTTPS é necessário para a configuração de produção dos
clientes. No Android debug, HTTP foi habilitado para desenvolvimento local.
No macOS foi adicionada a permissão de cliente de rede.

CORS permite `http://localhost:5173` e `http://127.0.0.1:5173`. Configure outras
origens por `Cors__Origins__0`, `Cors__Origins__1`, etc. CORS não é autenticação.
A documentação OpenAPI fica em `/openapi/v1.json` no ambiente Development;
`/health` confirma que a aplicação está atendendo.

## Tarefas concluídas

- BACK-01: conexão SQLite configurável, chaves estrangeiras e migration inicial.
- BACK-02/03/04: tabelas, relações, índices, restrições e exclusão em cascata dos
  vínculos de treino. Exercícios em uso têm exclusão bloqueada.
- BACK-05: listagem, detalhes, criação, atualização e exclusão de fichas.
- BACK-06: CRUD do catálogo de exercícios.
- BACK-07: catálogo inicial com IDs estáveis, inserido pela migration.
- BACK-08: validações na camada de serviço e restrições adicionais no banco.
- BACK-09: serviços de treino/exercício separados das rotas e da persistência.
- BACK-10: adapter HTTP do Flutter e UUIDs compartilhados com a API.
- BACK-11: testes de integração com SQLite real e verificação do adapter Dart.

## Contrato HTTP

| Método | Rota | Resultado |
|---|---|---|
| GET | /api/treinos | Fichas completas, ordenadas por dia/nome |
| GET | /api/treinos/{id} | Uma ficha com itens ordenados |
| POST | /api/treinos | Cria com UUID gerado pelo servidor (201 + Location) |
| PUT | /api/treinos/{id} | Cria ou substitui a ficha com UUID fornecido (200) |
| DELETE | /api/treinos/{id} | Exclui ficha e vínculos (204) |
| GET | /api/exercicios | Catálogo por grupo/nome |
| GET | /api/exercicios/{id} | Consulta um exercício |
| POST | /api/exercicios | Cria exercício (201 + Location) |
| PUT | /api/exercicios/{id} | Atualiza exercício existente |
| DELETE | /api/exercicios/{id} | Exclui se não estiver em uso (204) |

O Front usa PUT com UUID gerado uma vez por formulário. Repetir a gravação com o
mesmo ID não cria uma segunda ficha. IDs inválidos na rota não correspondem a um
recurso. IDs desconhecidos na consulta/exclusão retornam 404.

Exemplo de corpo para criar/salvar treino:

```json
{
  "nome": "Peito + Tríceps",
  "diaSemana": 1,
  "descricao": "Treino A",
  "ativo": true,
  "exercicios": [
    {
      "exercicioId": "00000000-0000-4000-8000-000000000001",
      "series": 4,
      "repeticoes": 10,
      "cargaInicial": 72.5,
      "tempoDescanso": 90
    }
  ]
}
```

A ordem é a posição no array; o servidor atribui `ordem` a partir de zero.
Respostas incluem o exercício completo em cada item, além de ID, ordem, séries,
repetições, carga e descanso. DTOs evitam ciclos das entidades de navegação.

## Regras e integridade

- Nome obrigatório, até 120 caracteres; grupo muscular até 80.
- Descrição opcional, até 2000 caracteres. Textos são aparados nas extremidades.
- Dia: 1 (segunda) a 7 (domingo).
- Ao menos um exercício, existente e sem duplicação na mesma ficha.
- Séries e repetições: inteiros maiores que zero.
- Carga: número finito, zero ou mais, em kg.
- Descanso: inteiro, zero ou mais, em segundos.
- Criação preserva `CreatedAt`; edição preenche `UpdatedAt`, em UTC.
- Edição remove os vínculos anteriores e insere os novos numa única transação.
  Qualquer falha desfaz inclusive as remoções já enviadas ao banco.
- Excluir treino mantém o catálogo. Excluir exercício em uso retorna 409.
- Validação retorna 400; ausência retorna 404; conflitos de integridade retornam
  409; erros inesperados retornam 500 sem expor os detalhes internos ao cliente.

## Testes

```sh
dotnet test tests/MoveUp.Tests.csproj
```

Seis testes com SQLite real em arquivos temporários cobrem CRUD, reordenação e
remoção de itens, validações, duplicações, reinício da API, preservação do seed,
exclusão restrita, rollback após falha na inserção, OpenAPI e CORS.

No Flutter:

```sh
flutter test
flutter analyze
# Com uma API de desenvolvimento rodando:
dart run tool/check_api.dart http://localhost:5013
```

A última verificação usa o adapter real para criar, ler, editar e excluir uma
ficha temporária. O catálogo não é modificado. Os testes da API usam banco isolado
e não acessam os dados normais da aplicação.

## Configuração privada

`.env` é local e ignorado pelo Git. O script `scripts/run-local.sh` carrega seus
valores; `dotnet run` executado diretamente não lê esse arquivo automaticamente.
O `.env.example` contém somente valores públicos de exemplo. Senhas, tokens e
chaves de serviços futuros devem ficar no ambiente do servidor ou no `.env`
local, nunca nos arquivos versionados. Não compartilhe o conteúdo do `.env`.
Bancos locais, certificados e arquivos de assinatura também estão ignorados.

## Fase 2 — execução e histórico de treinos

BACK-12 a BACK-19 concluídas. O Front pode usar o modo API (`USE_PREVIEW=false`)
para executar sessões com persistência. A migration `AddTrainingSessions` é
aplicada ao iniciar a API e mantém as fichas e o catálogo existentes.

### Modelo

- `SessaoTreino`: ID do cliente, vínculo opcional com a ficha, ID original para
  idempotência, nome copiado, início/fim UTC, status e observação.
- `SessaoExercicio`: nome, grupo, ordem e descanso copiados no início. Não depende
  do registro atual do catálogo.
- `SerieRealizada`: ordem, carga/repetições planejadas e realizadas, status e
  horário da conclusão.

A exclusão da ficha apenas limpa o vínculo opcional da sessão (`SET NULL`).
Nomes, exercícios e resultados anteriores permanecem. Editar ou excluir exercícios
do catálogo também não altera a cópia histórica.

### Rotas

| Método | Rota | Comportamento |
|---|---|---|
| GET | /api/sessoes/ativa | Sessão completa ou 204 se não houver ativa |
| GET | /api/sessoes/{id} | Consulta com exercícios, séries e resumo |
| PUT | /api/sessoes/{id} | Inicia com `{ "treinoId": "uuid" }` |
| PUT | /api/sessoes/{id}/series/{serieId} | Registra, pula ou reabre uma série |
| PUT | /api/sessoes/{id}/conclusao | Finaliza com observação opcional |
| PUT | /api/sessoes/{id}/cancelamento | Cancela a sessão |
| GET | /api/sessoes?pagina=1&tamanhoPagina=20&status=concluida | Histórico paginado |

O contrato JSON completo, já consumido pelo Flutter, está na
[documentação de sessões](https://github.com/JoaoPSchiavoni/MoveUpApp/blob/main/docs/phase-2-session-api.md).

### Regras de execução

- Apenas uma sessão ativa por vez no MVP individual. Um índice único filtrado
  no banco protege essa regra inclusive sob requisições simultâneas.
- Início copia a ficha em uma transação. Repetir o mesmo UUID retorna a mesma
  sessão e os mesmos IDs de exercícios/séries; reutilizar para outra ficha dá 409.
- Sessão: `emAndamento`, `concluida` ou `cancelada`.
- Série: `pendente`, `concluida` ou `pulada`.
- Concluir série exige repetições inteiras positivas e carga finita não negativa.
  Volume que excederia a capacidade numérica é rejeitado. Zero kg é permitido.
- Pular/reabrir série exige resultados nulos e preserva o planejamento. Repetir
  uma gravação idêntica não altera o horário de conclusão.
- Só é possível finalizar com pelo menos uma série concluída. As séries restantes
  são marcadas como puladas. Observação opcional até 2000 caracteres.
- Conclusão/cancelamento repetidos retornam o mesmo encerramento. Não é permitido
  alterar séries encerradas, cancelar sessão concluída ou concluir cancelada.
- Transações serializam as alterações de séries e encerramento, impedindo salvar
  resultados depois da conclusão. Falhas revertem toda a operação.
- Histórico contém somente concluídas, em ordem decrescente de início e ID.
  Tamanho de página entre 1 e 100; sessão com até 1000 séries.
- Resumo: duração em segundos entre início/fim, exercícios com séries concluídas,
  contagem de concluídas/puladas e soma de carga × repetições realizadas.

### Validação da fase 2

`dotnet test tests/MoveUp.Tests.csproj` executa 15 testes (6 da fase 1 e 9 da fase 2),
com bancos temporários. Cobertura: fluxo completo, reabertura/pulo de séries,
validações, pertencimento de séries, tentativas repetidas, concorrência, retomada
após reinício, histórico, preservação após excluir ficha/catálogo, migração da
fase 1 e rollback após falha forçada na criação da sessão.

No Flutter, `tool/check_session_api.dart` verifica os adapters HTTP reais contra
uma API de teste isolada. Ele cria e conclui uma sessão e remove sua ficha de
origem; o histórico permanece no banco temporário. O banco pessoal não é usado.

## Fase 3 — calendário e consistência

BACK-20 a BACK-27 implementadas e integradas ao Flutter (FRONT-19 a FRONT-26).
A migration `AddConsistency` preserva fichas, catálogo e sessões, adicionando
preferências de acompanhamento, revisões da rotina/meta e data/fuso de presença.

- Na Home ou Calendário, confirme os dias planejados, o fuso IANA e uma meta
  semanal entre 1 e 7 dias. Fichas ativas sugerem a configuração inicial.
- A primeira ativação classifica o histórico no fuso escolhido, sem presumir
  faltas antigas. Novas sessões fixam sua data local ao iniciar o treino.
- Somente sessões concluídas com alguma série realizada contam como presença.
  Duas sessões no mesmo dia contam um dia treinado, mantendo ambas nos detalhes.
- Descanso não quebra a sequência; treinos extras contam para a meta, sem
  aumentar a sequência de dias planejados. Hoje só vira falta após encerrar.
- Mudanças de rotina valem a partir do dia seguinte; metas, da próxima segunda.
  As revisões preservam o passado. Mudar o fuso não move presenças antigas nem
  antecipa revisões futuras já agendadas.
- O resumo semanal apresenta dias, sessões, duração, volume e adesão aos dias
  planejados já encerrados. Sem denominador, adesão é nula.

| Método | Rota | Resultado |
|---|---|---|
| GET/PUT | /api/acompanhamento/configuracao | Preferências, rotina/meta e revisões com vigência |
| GET | /api/acompanhamento/calendario?ano=2026&mes=10 | Datas classificadas e referências das sessões |
| GET | /api/acompanhamento/painel | Semana/mês atuais, sequência atual e melhor |
| GET | /api/acompanhamento/semanas?inicio=2026-09-28 | Resumo da semana iniciada nessa segunda-feira |

Datas de calendário/vigência usam `YYYY-MM-DD`. Horários de sessão continuam UTC.
O Back calcula as métricas usando `TimeProvider` e o fuso configurado. As consultas
por mês/semana projetam dados do período; a sequência usa datas distintas de
presença e versões da rotina, sem carregar todas as séries do histórico.

`dotnet test tests/MoveUp.Tests.csproj` passa 21 testes, incluindo 6 cenários novos
com SQLite isolado para frequência, rotina, meta, validações, fusos e reinício.
O Flutter passa 21 testes, análise estática e compilação web. A integração real
foi verificada com os adapters Dart e banco temporário vazio.

Contrato e execução no [plano de calendário e consistência](https://github.com/JoaoPSchiavoni/MoveUpApp/blob/main/docs/phase-3-consistency-plan.md).
O acompanhamento remoto requer `USE_REMOTE_API=true` e `USE_PREVIEW=false`.
O modo padrão local possui acompanhamento equivalente sem API. Nenhum segredo
adicional é necessário.


## Expansão após o MVP — origem e OnFire

A migration `PreserveExerciseIdentity` adiciona `ExercicioOrigemId` aos snapshots
de exercícios. Novas sessões preservam o ID do catálogo mesmo após alterações ou
exclusão da ficha/exercício. Sessões antigas mantêm `null`, sem associação inventada.
As respostas de sessão incluem também `treinoOrigemId`, `dataPresenca` e
`fusoPresenca`, permitindo importar o histórico para o SQLite do celular sem
perder as datas já fixadas.

O calendário passa a retornar `onFire` em cada dia: após cinco dias planejados
cumpridos em sequência, os dias realizados do segmento ganham destaque. Descanso
preserva a sequência; falta encerrada a quebra. Sessões duplicadas não inflam o
contador. Segmentos históricos que alcançaram cinco permanecem destacados.

`dotnet test tests/MoveUp.Tests.csproj` passa 23 testes. Os cenários adicionais
verificam OnFire entre meses e a migração de histórico da fase 3 preservando
resultados e datas, sem inventar identidade para exercícios legados. A preservação
de origem após excluir ficha/catálogo também é verificada.

Modelos, tema, ilustrações, gráficos, medidas e backup desta expansão ficam no
Flutter e no banco local. Esta API não oferece contas nem sincronização automática.
