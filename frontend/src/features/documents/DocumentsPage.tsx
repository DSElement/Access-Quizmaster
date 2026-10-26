import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useRef, useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { DocumentStatusBadge } from '../../components/Badge'
import { Empty, ErrorNote, Loading } from '../../components/LoadState'
import { PageHeader } from '../../components/PageHeader'
import { formatBytes, formatDateTime, plural } from '../../lib/format'
import {
  ACCEPTED_EXTENSIONS,
  documentKeys,
  fetchDocuments,
  findFileProblem,
  isIndexing,
  POLL_INTERVAL_MS,
  uploadDocument,
} from './api'
import styles from './DocumentsPage.module.css'

export function DocumentsPage() {
  const documents = useQuery({
    queryKey: documentKeys.list,
    queryFn: ({ signal }) => fetchDocuments(signal),
    // Keep polling only while something is still being indexed.
    refetchInterval: (query) => (query.state.data?.some(isIndexing) ? POLL_INTERVAL_MS : false),
  })

  return (
    <section>
      <PageHeader title="Documents" />

      <UploadForm />

      {documents.isPending ? (
        <Loading label="Loading documents…" />
      ) : documents.isError ? (
        <ErrorNote error={documents.error} />
      ) : documents.data.length === 0 ? (
        <Empty>
          <p>You haven't uploaded any documents yet.</p>
        </Empty>
      ) : (
        <ul className={styles.list}>
          {documents.data.map((document) => (
            <li key={document.id}>
              <Link to={`/documents/${document.id}`} className={styles.row}>
                <span className={styles.name}>{document.fileName}</span>
                <span className={styles.meta}>
                  {formatBytes(document.sizeBytes)} · {formatDateTime(document.uploadedAt)}
                  {document.status === 'Ready' && ` · ${plural(document.chunkCount, 'chunk')}`}
                </span>
                <span className={styles.status}>
                  <DocumentStatusBadge status={document.status} />
                </span>
                {document.status === 'Failed' && document.error && (
                  <span className={styles.error}>{document.error}</span>
                )}
              </Link>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

function UploadForm() {
  const queryClient = useQueryClient()
  const inputRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [problem, setProblem] = useState<string | null>(null)

  const upload = useMutation({
    mutationFn: uploadDocument,
    onSuccess: () => {
      setFile(null)
      if (inputRef.current) inputRef.current.value = ''
      return queryClient.invalidateQueries({ queryKey: documentKeys.list })
    },
  })

  function choose(chosen: File | null) {
    upload.reset()
    setFile(chosen)
    setProblem(chosen && findFileProblem(chosen))
  }

  function submit(event: FormEvent) {
    event.preventDefault()
    if (file && !problem) upload.mutate(file)
  }

  return (
    <form className={styles.upload} onSubmit={submit}>
      <label htmlFor="document-file" className={styles.uploadLabel}>
        Upload a document
      </label>
      <div className={styles.uploadRow}>
        <input
          ref={inputRef}
          id="document-file"
          type="file"
          accept={ACCEPTED_EXTENSIONS.join(',')}
          className={styles.fileInput}
          aria-describedby="document-file-hint"
          aria-invalid={problem ? true : undefined}
          onChange={(event) => choose(event.target.files?.[0] ?? null)}
        />
        <button type="submit" disabled={!file || !!problem || upload.isPending}>
          {upload.isPending ? 'Uploading…' : 'Upload'}
        </button>
      </div>
      <p id="document-file-hint" className={styles.hint}>
        PDF, Markdown or plain text, up to 10 MB. Scanned PDFs (pictures of pages) can't be read.
      </p>
      {problem && (
        <p role="alert" className={styles.problem}>
          {problem}
        </p>
      )}
      {upload.isError && <ErrorNote error={upload.error} />}
    </form>
  )
}
