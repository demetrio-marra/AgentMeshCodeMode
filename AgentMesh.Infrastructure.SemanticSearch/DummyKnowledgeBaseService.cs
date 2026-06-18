using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;

namespace AgentMesh.Infrastructure.SemanticSearch
{
    public class DummyKnowledgeBaseService : IKnowledgeBaseService
    {
        static readonly Guid fakeGuid1 = Guid.Parse("65d94efd-6bd3-43bd-89e0-69295d6ce87f");
        static readonly Guid fakeGuid2 = Guid.Parse("db1d4b23-ff2d-4331-8b29-44de903ced30");

        readonly IEnumerable<KnowledgeBaseQueryResult> dummyresults = new List<KnowledgeBaseQueryResult>() {
            new KnowledgeBaseQueryResult {
                 SearchTerm = "Situazione contabile",
                Id = fakeGuid1.ToString(),
                Title = $"GetSituazioneContabileCompleta API Description",
                Summary = $"Restituisce la situazione contabile completa per un singolo Cliente di Studio, riferita ad un singolo periodo contabile.",
                RelevanceScore = 1.0f
            },
            new KnowledgeBaseQueryResult {
                 SearchTerm = "User data",
                Id = fakeGuid2.ToString(),
                Title = $"GetUserByUsername API Description",
                Summary = $"Restituisce l'utente dall'username.",
                RelevanceScore = 0.4f
            }
            };


        public async Task<IEnumerable<KnowledgeBaseQueryResult>> KeywordsSearch(IEnumerable<string> searchTerms, CancellationToken cancellationToken = default)
        {
            var results = dummyresults;
            return await Task.FromResult(results);
        }

        public async Task<IEnumerable<KnowledgeBaseQueryResult>> SemanticSearchAsync(IEnumerable<string> searchTerms, bool rerank = false, CancellationToken cancellationToken = default)
        {
            var results = dummyresults;
            return await Task.FromResult(results);
        }

        private const string Dummycontent = @"# GetSituazioneContabileCompleta API Description
Restituisce la situazione contabile completa per un singolo Cliente di Studio, riferita ad un singolo periodo contabile.

## Documentation for Business Analyst

### Recommended Use Cases
- L'utente richiede genericamente la 'situazione contabile' per un Cliente di Studio.

### Less Suitable Use Cases
- Richieste per Clienti di Studio e/o Periodi multipli. Utilizzare il tool più volte, fino ad un massimo di 5 volte per singola richiesta
- Situazione contabile di un singolo conto

### Unsupported Use Cases
- Estrazione dei sottoconti (a volte anche chiamati clienti/fornitori)

### Required parameters
- IdClienteDiStudio (numerico): id del cliente di studio. Se l'utente ha fornito solo la denominazione del Cliente di Studio, utilizzare il tool `search_customer` per ottenere l'IdClienteDiStudio
- IdPeriod (numerico): id del periodo. Se l'utente non lo ha fornito, utilizza il tool `get_period` per recupe l'IdPeriod

### Returned data
Elenco dei conti. Ciascun elemento contiene:
- Codice conto (numerico): identificativo univoco conto
- Descrizione conto (testo): descrizione parlante conto
- Natura conto (enum): tipo conto (Costo/Ricavo/Passivo/Attivo)
- Dare (importo): importo Dare
- Avere (importo): importo Avere
- Saldo (importo): saldo conto

### Optional capabilities
- Flag ripresa saldi: riporta i saldi del periodo precedente in continuità con il periodo corrente
- Flag saldi per cassa: riporta i saldi utilizzando la logica di 'cassa' invece che di 'competenza'


## Documentation for Developer

### API Signature
```typescript
async getSituazioneContabileCompleta(int idClienteDiStudio, int idPeriod, bool? saldiPerCassa = false)
```

### Returned DTO
```typescript
{
    codiceConto: string,
    descrizioneConto: string,
    naturaConto: string,
    dare: number,
    avere: number,
    saldo: number
}[]
```

### Usage sample

```javascript

... // omitted previous code

try {
  let getSituazioneContabileCompletaResult = await getSituazioneContabileCompleta(idPeriod, idClienteDiStudio);

  // eg: the user asked only for saldo
  let mappedResult = getSituazioneContabileCompletaResult.map(d => ({
    codiceConto: d.codiceConto,
    descrizioneConto: d.descrizioneConto,
    naturaConto: d.naturaConto,
    saldo: d.saldo
  }));

  return mappedResult;

} catch (e) {
  return 'An error occurred using getSituazioneContabileCompleta: ' + e.Description;
}

```";

        public async Task<string> GetKnowledgeBaseEntryContentAsync(string id, CancellationToken cancellationToken = default)
        {
            var result = Dummycontent;
            return await Task.FromResult(result);
        }


        public async Task<IDictionary<string, string?>> GetKnowledgeBaseEntriesContentAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            var result = ids.ToDictionary(
                id => id,
                id => (string?)Dummycontent);
            return await Task.FromResult(result);
        }
    }
}
