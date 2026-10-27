import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router'
import { ApiError } from '../../api/client'
import type { DocumentSummary } from '../../api/types'
import { DocumentStatusBadge } from '../../components/Badge'
import { ErrorNote, Loading } from '../../components/LoadState'
import { PageHeader } from '../../components/PageHeader'
import { Spinner } from '../../components/Spinner'
import { formatBytes, formatDateTime } from '../../lib/format'
import {
  deleteDocument,
  documentKeys,
  fetchChunks,
  fetchDocument,
  generateQuiz,
  isIndexing,
  MAX_FILE_NAME_LENGTH,
  POLL_INTERVAL_MS,
  QUESTION_COUNTS,
  renameDocument,
  searchDocument,
  splitFileName,
} from './api'
import styles from './DocumentPage.module.css'
import { IndexingStatus } from './IndexingStatus'

export function DocumentPage() {
  const { id = '' } = useParams()
  const document = useQuery({
    queryKey: documentKeys.detail(id),
    queryFn: ({ signal }) => fetchDocument(id, signal),
    refetchInterval: (query) => (query.state.data && isIndexing(query.state.data) ? POLL_INTERVAL_MS : false),
  })

  if (document.isPending) return <Loading label="Loading document…" />
  if (document.isError) {
    return <ErrorNote error={document.error} messages={{ 404: "This document doesn't exist, or it isn't yours." }} />
  }

  return <DocumentDetail document={document.data} />
}

function DocumentDetail({ document }: { document: DocumentSummary }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [confirmingDelete, setConfirmingDelete] = useState(false)

  const remove = useMutation({
    mutationFn: () => deleteDocument(document.id),
    onSuccess: () => {
      navigate('/documents', { replace: true })
      queryClient.removeQueries({ queryKey: documentKeys.detail(document.id) })
      queryClient.removeQueries({ queryKey: documentKeys.chunks(document.id) })
      return queryClient.invalidateQueries({ queryKey: documentKeys.list })
    },
  })

  return (
    <article>
      <PageHeader eyebrow={<DocumentStatusBadge status={document.status} />} title={document.fileName} />

      <dl className={styles.facts}>
        <div>
          <dt>Size</dt>
          <dd>{formatBytes(document.sizeBytes)}</dd>
        </div>
        <div>
          <dt>Uploaded</dt>
          <dd>{formatDateTime(document.uploadedAt)}</dd>
        </div>
        {document.status === 'Ready' && (
          <div>
            <dt>Chunks</dt>
            <dd>{document.chunkCount}</dd>
          </div>
        )}
      </dl>

      {isIndexing(document) && <IndexingStatus document={document} />}

      {document.status === 'Failed' && (
        <p className={styles.failed} role="alert">
          {document.error ?? 'Indexing failed.'}
        </p>
      )}

      {document.status === 'Ready' && (
        <>
          <GenerateQuiz documentId={document.id} />
          <SearchBox documentId={document.id} />
          <ChunkList documentId={document.id} count={document.chunkCount} />
        </>
      )}

      <section className={styles.manage} aria-label="Manage">
        <RenameDocument document={document} />
        {confirmingDelete ? (
          <span className={styles.confirm}>
            <span>Delete the document and its chunks?</span>
            <button type="button" className="danger" onClick={() => remove.mutate()} disabled={remove.isPending}>
              {remove.isPending ? 'Deleting…' : 'Yes, delete'}
            </button>
            <button type="button" className="secondary" onClick={() => setConfirmingDelete(false)}>
              Keep it
            </button>
          </span>
        ) : (
          <button type="button" className="danger" onClick={() => setConfirmingDelete(true)}>
            Delete
          </button>
        )}
        {remove.isError && <ErrorNote error={remove.error} />}
      </section>
    </article>
  )
}

function RenameDocument({ document }: { document: DocumentSummary }) {
  const queryClient = useQueryClient()
  const { name, extension } = splitFileName(document.fileName)
  const [editing, setEditing] = useState(false)
  const [draft, setDraft] = useState(name)

  const rename = useMutation({
    mutationFn: (next: string) => renameDocument(document.id, next),
    onSuccess: (updated) => {
      queryClient.setQueryData(documentKeys.detail(document.id), updated)
      setEditing(false)
      return queryClient.invalidateQueries({ queryKey: documentKeys.list })
    },
  })

  // A validation error comes back on the "name" field; anything else falls back to the general message.
  const errorMessage =
    rename.error instanceof ApiError
      ? (rename.error.fieldErrors.name?.join(' ') ?? rename.error.message)
      : rename.error?.message

  function start() {
    setDraft(name)
    rename.reset()
    setEditing(true)
  }

  function submit(event: FormEvent) {
    event.preventDefault()
    rename.mutate(draft.trim())
  }

  if (!editing) {
    return (
      <button type="button" className="secondary" onClick={start}>
        Rename
      </button>
    )
  }

  const unchanged = draft.trim() === name

  return (
    <form className={styles.renameForm} onSubmit={submit}>
      <label className={`${styles.field} ${styles.grow}`}>
        <span className={styles.fieldLabel}>New name</span>
        <span className={styles.nameRow}>
          <input
            className={`${styles.input} ${styles.grow}`}
            value={draft}
            maxLength={MAX_FILE_NAME_LENGTH - extension.length}
            onChange={(event) => setDraft(event.target.value)}
            autoFocus
          />
          {extension && <span className={styles.extension}>{extension}</span>}
        </span>
      </label>
      <button type="submit" disabled={!draft.trim() || unchanged || rename.isPending}>
        {rename.isPending ? 'Saving…' : 'Save name'}
      </button>
      <button type="button" className="secondary" onClick={() => setEditing(false)}>
        Cancel
      </button>
      {errorMessage && (
        <p role="alert" className={`${styles.failed} ${styles.fullRow}`}>
          {errorMessage}
        </p>
      )}
    </form>
  )
}

function GenerateQuiz({ documentId }: { documentId: string }) {
  const navigate = useNavigate()
  const [questionCount, setQuestionCount] = useState(5)
  const [topic, setTopic] = useState('')

  const generate = useMutation({
    mutationFn: () => generateQuiz(documentId, { questionCount, topic: topic.trim() || null }),
    // Nothing is saved yet: the draft opens in the quiz builder for review.
    onSuccess: (draft) => navigate('/quizzes/new', { state: { draft } }),
  })

  function submit(event: FormEvent) {
    event.preventDefault()
    generate.mutate()
  }

  return (
    <section className={styles.section} aria-labelledby="generate-heading">
      <h2 id="generate-heading" className={styles.heading}>
        Generate a quiz
      </h2>
      <p className={styles.note}>AI writes a draft from this document. You review and edit it before it's saved.</p>

      <form className={styles.generateForm} onSubmit={submit}>
        <label className={styles.field}>
          <span className={styles.fieldLabel}>Questions</span>
          <select
            className={styles.input}
            value={questionCount}
            onChange={(event) => setQuestionCount(Number(event.target.value))}
          >
            {QUESTION_COUNTS.map((count) => (
              <option key={count} value={count}>
                {count}
              </option>
            ))}
          </select>
        </label>
        <label className={`${styles.field} ${styles.grow}`}>
          <span className={styles.fieldLabel}>Focus (optional)</span>
          <input
            className={styles.input}
            value={topic}
            maxLength={200}
            onChange={(event) => setTopic(event.target.value)}
            placeholder="e.g. the Calvin cycle"
          />
        </label>
        <button type="submit" disabled={generate.isPending}>
          {generate.isPending ? (
            <>
              <Spinner /> Writing the quiz…
            </>
          ) : (
            'Generate draft'
          )}
        </button>
      </form>

      {generate.isPending && <p className={styles.note}>This usually takes under a minute.</p>}
      {generate.isError && <ErrorNote error={generate.error} />}
    </section>
  )
}

function SearchBox({ documentId }: { documentId: string }) {
  const [draft, setDraft] = useState('')
  const [query, setQuery] = useState('')

  const results = useQuery({
    queryKey: documentKeys.search(documentId, query),
    queryFn: ({ signal }) => searchDocument(documentId, query, signal),
    enabled: query.length > 0,
    staleTime: Infinity,
  })

  function submit(event: FormEvent) {
    event.preventDefault()
    const next = draft.trim()
    // Setting the same query again changes nothing, so a repeated search (e.g. after an error) refetches instead.
    if (next === query) void results.refetch()
    else setQuery(next)
  }

  return (
    <section className={styles.section} aria-labelledby="search-heading">
      <h2 id="search-heading" className={styles.heading}>
        Search
      </h2>
      <p className={styles.note}>Ask a question to see which chunks match it best.</p>

      <form className={styles.searchRow} onSubmit={submit} role="search">
        <label htmlFor="chunk-search" className="visually-hidden">
          Question
        </label>
        <input
          id="chunk-search"
          type="search"
          className={`${styles.input} ${styles.grow}`}
          value={draft}
          maxLength={500}
          onChange={(event) => setDraft(event.target.value)}
          placeholder="e.g. What happens in the Calvin cycle?"
        />
        <button type="submit" className="secondary" disabled={!draft.trim() || results.isFetching}>
          {results.isFetching ? 'Searching…' : 'Search'}
        </button>
      </form>

      {results.isError && <ErrorNote error={results.error} />}
      {results.data && (
        <ol className={styles.chunks}>
          {results.data.map((match) => (
            <li key={match.chunkId} className={styles.chunk}>
              <p className={styles.chunkLabel}>
                Chunk {match.index + 1} · similarity {match.similarity.toFixed(2)}
              </p>
              <p className={styles.chunkText}>{match.text}</p>
            </li>
          ))}
        </ol>
      )}
    </section>
  )
}

function ChunkList({ documentId, count }: { documentId: string; count: number }) {
  const [open, setOpen] = useState(false)
  const chunks = useQuery({
    queryKey: documentKeys.chunks(documentId),
    queryFn: ({ signal }) => fetchChunks(documentId, signal),
    enabled: open,
    staleTime: Infinity,
  })

  return (
    <details className={styles.section} onToggle={(event) => setOpen(event.currentTarget.open)}>
      <summary className={styles.summary}>Show all {count} chunks</summary>

      {chunks.isPending && open && <Loading label="Loading chunks…" />}
      {chunks.isError && <ErrorNote error={chunks.error} />}
      {chunks.data && (
        <ol className={styles.chunks}>
          {chunks.data.map((chunk) => (
            <li key={chunk.id} className={styles.chunk}>
              <p className={styles.chunkLabel}>
                Chunk {chunk.index + 1} · {chunk.text.length} characters
              </p>
              <p className={styles.chunkText}>{chunk.text}</p>
            </li>
          ))}
        </ol>
      )}
    </details>
  )
}
