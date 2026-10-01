# MoveUp — Back da fase 1

API ASP.NET Core 10, Entity Framework Core e SQLite, integrada ao aplicativo
Flutter em `../moveupapp`. O projeto já iniciado em C# foi completado mantendo as
entidades `Treino`, `Exercicio` e `TreinoExercicio`.

## Arquitetura e limite desta entrega

Flutter → ApiWorkoutGateway → Controllers → Services → Repositories → SQLite.

O SQLite fica **na máquina da API**, em `App_Data/moveup.db`. O app precisa de
conexão com a API; esta implementação não é armazenamento offline no celular.
O plano inicial com Drift dentro do Flutter continua sendo uma arquitetura
alternativa, não implementada aqui. A API é de uso local/de desenvolvimento,
sem autenticação ou separação de dados por usuário nesta fase.

## Executar

Na pasta MoveUp:

```sh
cp .env.example .env # Apenas na primeira configuração
./scripts/run-local.sh
```

A API atende em `http://localhost:5013`. A primeira inicialização aplica a migration
`InitialCreate`, cria o banco e insere 12 exercícios. Inicializações seguintes
não duplicam nem sobrescrevem o catálogo. Alterações futuras de esquema devem
usar novas migrations, sem apagar o banco.

Para configurar o arquivo, use `Database__Path=/caminho/moveup.db` no ambiente.
O caminho relativo é resolvido a partir da pasta do projeto. O arquivo antigo
`fitnessapp.db`, se existir na pasta local do sistema, não é alterado ou importado
automaticamente. Este projeto usa um novo banco versionado por migrations;
bancos antigos criados com EnsureCreated precisam de migração de dados planejada.

No projeto Flutter:

```sh
flutter run -d chrome --web-hostname localhost --web-port 5173
```

O app usa a API por padrão. No emulador Android, o endereço padrão é
`http://10.0.2.2:5013`; nas outras plataformas, `http://localhost:5013`.
Para endereço diferente:

```sh
flutter run --dart-define=API_BASE_URL=https://seu-host
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
