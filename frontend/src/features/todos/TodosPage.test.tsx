import { render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { TodosPage } from './TodosPage'

describe('TodosPage', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  // AK-3.3: Givet att API:t inte svarar, när arbetslistan laddas i appen,
  // så visas ett felmeddelande, inte en tom lista.
  it('AK-3.3: shows an error message, not an empty list, when the API does not answer', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new TypeError('Failed to fetch'))

    render(<TodosPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent(/kunde inte hämta/i)
    expect(screen.queryByText(/inga uppgifter än/i)).not.toBeInTheDocument()
  })

  it('AK-3.3: shows an error message when the API answers with an error', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 500 }))

    render(<TodosPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent(/kunde inte hämta/i)
    expect(screen.queryByText(/inga uppgifter än/i)).not.toBeInTheDocument()
  })
})
