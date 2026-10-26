const dateFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' })
const dateTimeFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' })

export const formatDate = (iso: string) => dateFormat.format(new Date(iso))

export const formatDateTime = (iso: string) => dateTimeFormat.format(new Date(iso))

export const plural = (count: number, word: string) => `${count} ${count === 1 ? word : `${word}s`}`
