import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { useId } from 'react'
import { useFieldArray, useForm, useWatch, type Control, type UseFormReturn } from 'react-hook-form'
import { Link, useNavigate } from 'react-router'
import { setServerErrors } from '../../api/formErrors'
import type { EditableQuiz, SaveQuizInput } from '../../api/types'
import { TextAreaField, TextField } from '../../components/TextField'
import { plural } from '../../lib/format'
import { quizKeys } from './api'
import styles from './QuizForm.module.css'
import {
  emptyQuestion,
  isQuizFormField,
  MAX_OPTIONS,
  MAX_QUESTIONS,
  MIN_OPTIONS,
  quizFormSchema,
  toSaveInput,
  type QuizFormValues,
} from './quizSchema'

type Props = {
  defaultValues: QuizFormValues
  submitLabel: string
  cancelTo: string
  save: (input: SaveQuizInput) => Promise<EditableQuiz>
}

const arrayError = (error: unknown) => {
  const e = error as { message?: string; root?: { message?: string } } | undefined
  return e?.root?.message ?? e?.message
}

export function QuizForm({ defaultValues, submitLabel, cancelTo, save }: Props) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  const form = useForm<QuizFormValues>({ resolver: zodResolver(quizFormSchema), defaultValues })
  const {
    control,
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = form

  const questions = useFieldArray({ control, name: 'questions' })

  const onSubmit = handleSubmit(async (values) => {
    try {
      const saved = await save(toSaveInput(values))
      await queryClient.invalidateQueries({ queryKey: quizKeys.all })
      navigate(`/quizzes/${saved.id}`)
    } catch (error) {
      setServerErrors(error, setError, isQuizFormField)
    }
  })

  return (
    <form className={styles.form} onSubmit={onSubmit} noValidate>
      <div className={styles.details}>
        <TextField label="Title" error={errors.title?.message} {...register('title')} />
        <TextAreaField
          label="Description"
          hint="Optional. One or two sentences about what the quiz covers."
          rows={2}
          error={errors.description?.message}
          {...register('description')}
        />
      </div>

      <ol className={styles.questions}>
        {questions.fields.map((field, index) => (
          <QuestionEditor
            key={field.id}
            form={form}
            index={index}
            total={questions.fields.length}
            onMove={(to) => questions.move(index, to)}
            onRemove={() => questions.remove(index)}
          />
        ))}
      </ol>

      {arrayError(errors.questions) && (
        <p role="alert" className={styles.error}>
          {arrayError(errors.questions)}
        </p>
      )}

      <button
        type="button"
        className={`secondary ${styles.addQuestion}`}
        onClick={() => questions.append(emptyQuestion())}
        disabled={questions.fields.length >= MAX_QUESTIONS}
      >
        + Add question
      </button>

      <div className={styles.footer}>
        {errors.root?.server && (
          <p role="alert" className={styles.serverError}>
            {errors.root.server.message}
          </p>
        )}
        <span className={styles.count}>{plural(questions.fields.length, 'question')}</span>
        <Link to={cancelTo} className="button secondary">
          Cancel
        </Link>
        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : submitLabel}
        </button>
      </div>
    </form>
  )
}

type QuestionEditorProps = {
  form: UseFormReturn<QuizFormValues>
  index: number
  total: number
  onMove: (to: number) => void
  onRemove: () => void
}

function QuestionEditor({ form, index, total, onMove, onRemove }: QuestionEditorProps) {
  const {
    control,
    register,
    setValue,
    trigger,
    formState: { errors, isSubmitted },
  } = form
  const options = useFieldArray({ control, name: `questions.${index}.options` })
  const questionErrors = errors.questions?.[index]
  const number = index + 1

  const markCorrect = (correctIndex: number) => {
    options.fields.forEach((_, i) =>
      setValue(`questions.${index}.options.${i}.isCorrect`, i === correctIndex, { shouldDirty: true }),
    )
    if (isSubmitted) void trigger(`questions.${index}.options`)
  }

  return (
    <li className={styles.question}>
      <div className={styles.questionHeader}>
        <span className={styles.number}>Q{number}</span>
        <div className={styles.questionTools}>
          <button
            type="button"
            className="secondary"
            onClick={() => onMove(index - 1)}
            disabled={index === 0}
            aria-label={`Move question ${number} up`}
          >
            ↑
          </button>
          <button
            type="button"
            className="secondary"
            onClick={() => onMove(index + 1)}
            disabled={index === total - 1}
            aria-label={`Move question ${number} down`}
          >
            ↓
          </button>
          <button type="button" className="secondary" onClick={onRemove} disabled={total === 1}>
            Remove
          </button>
        </div>
      </div>

      <TextAreaField
        label={`Question ${number}`}
        rows={2}
        error={questionErrors?.text?.message}
        {...register(`questions.${index}.text`)}
      />

      <fieldset className={styles.options}>
        <legend className={styles.optionsLegend}>Answers — select the correct one</legend>
        {options.fields.map((option, optionIndex) => (
          <OptionRow
            key={option.id}
            control={control}
            questionIndex={index}
            optionIndex={optionIndex}
            groupName={`correct-${index}`}
            textError={questionErrors?.options?.[optionIndex]?.text?.message}
            onMarkCorrect={() => markCorrect(optionIndex)}
            onRemove={options.fields.length > MIN_OPTIONS ? () => options.remove(optionIndex) : undefined}
            register={register}
          />
        ))}

        {arrayError(questionErrors?.options) && (
          <p role="alert" className={styles.error}>
            {arrayError(questionErrors?.options)}
          </p>
        )}

        {options.fields.length < MAX_OPTIONS && (
          <button
            type="button"
            className={`secondary ${styles.addOption}`}
            onClick={() => options.append({ text: '', isCorrect: false })}
          >
            + Add answer
          </button>
        )}
      </fieldset>
    </li>
  )
}

type OptionRowProps = {
  control: Control<QuizFormValues>
  register: UseFormReturn<QuizFormValues>['register']
  questionIndex: number
  optionIndex: number
  groupName: string
  textError?: string
  onMarkCorrect: () => void
  onRemove?: () => void
}

function OptionRow({
  control,
  register,
  questionIndex,
  optionIndex,
  groupName,
  textError,
  onMarkCorrect,
  onRemove,
}: OptionRowProps) {
  const isCorrect = useWatch({ control, name: `questions.${questionIndex}.options.${optionIndex}.isCorrect` })
  const label = `Answer ${optionIndex + 1}`
  const errorId = useId()

  return (
    <div className={styles.option} data-correct={isCorrect || undefined}>
      <input
        type="radio"
        name={groupName}
        className={styles.correctRadio}
        checked={isCorrect === true}
        onChange={onMarkCorrect}
        aria-label={`${label} is correct`}
      />
      <div className={styles.optionInput}>
        <input
          className={styles.optionText}
          aria-label={label}
          aria-invalid={textError ? true : undefined}
          aria-describedby={textError ? errorId : undefined}
          placeholder={label}
          {...register(`questions.${questionIndex}.options.${optionIndex}.text`)}
        />
        {textError && (
          <p id={errorId} className={styles.error}>
            {textError}
          </p>
        )}
      </div>
      {isCorrect && <span className={styles.correctTag}>correct</span>}
      {onRemove && (
        <button type="button" className={`secondary ${styles.removeOption}`} onClick={onRemove} aria-label={`Remove ${label.toLowerCase()}`}>
          ×
        </button>
      )}
    </div>
  )
}
