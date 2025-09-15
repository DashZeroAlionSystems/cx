package main

import (
	"fmt"
)

// Domain

type Order struct {
	ID     string
	Amount int
}

type OrderRepository interface {
	Save(order Order) error
	FindByID(id string) (Order, bool)
}

type PaymentGateway interface {
	Charge(amount int) error
}

// Application service

type CheckoutService struct {
	repo    OrderRepository
	paygate PaymentGateway
}

func NewCheckoutService(repo OrderRepository, paygate PaymentGateway) *CheckoutService {
	return &CheckoutService{repo: repo, paygate: paygate}
}

func (s *CheckoutService) Checkout(order Order) error {
	if order.Amount <= 0 {
		return fmt.Errorf("invalid amount")
	}
	if err := s.paygate.Charge(order.Amount); err != nil {
		return fmt.Errorf("payment failed: %w", err)
	}
	if err := s.repo.Save(order); err != nil {
		return fmt.Errorf("persist failed: %w", err)
	}
	return nil
}

// Adapters (infrastructure)

type InMemoryOrderRepo struct { store map[string]Order }

func NewInMemoryOrderRepo() *InMemoryOrderRepo { return &InMemoryOrderRepo{store: map[string]Order{}} }

func (r *InMemoryOrderRepo) Save(order Order) error {
	r.store[order.ID] = order
	return nil
}

func (r *InMemoryOrderRepo) FindByID(id string) (Order, bool) {
	o, ok := r.store[id]
	return o, ok
}

type FakeGateway struct{ shouldFail bool }

func (g *FakeGateway) Charge(amount int) error {
	if g.shouldFail {
		return fmt.Errorf("gateway unavailable")
	}
	return nil
}

func main() {
	repo := NewInMemoryOrderRepo()
	gateway := &FakeGateway{shouldFail: false}
	service := NewCheckoutService(repo, gateway)
	order := Order{ID: "o-1", Amount: 100}
	if err := service.Checkout(order); err != nil {
		panic(err)
	}
	if saved, ok := repo.FindByID("o-1"); ok {
		fmt.Println("Saved order:", saved.ID, saved.Amount)
	}
}